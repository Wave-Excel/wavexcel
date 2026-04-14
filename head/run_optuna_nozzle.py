#!/usr/bin/env python3
from __future__ import annotations

import argparse
import dataclasses
import json
import math
import os
import shlex
import subprocess
import sys
import time
from typing import Any, Dict, Optional, Tuple

import optuna


@dataclasses.dataclass(frozen=True)
class ParamSpec:
    name: str
    low: float
    high: float
    step: Optional[float]
    kind: str  # "float" | "int"

    def suggest(self, trial: optuna.Trial) -> float:
        if self.kind == "int":
            step_int = int(self.step) if self.step else 1
            return float(trial.suggest_int(self.name, int(self.low), int(self.high), step=step_int))
        if self.step and self.step > 0:
            return float(trial.suggest_float(self.name, self.low, self.high, step=self.step))
        return float(trial.suggest_float(self.name, self.low, self.high))


def _parse_json_from_stdout(stdout: str) -> Dict[str, Any]:
    s = stdout.strip()
    # allow extra lines but find the first JSON object
    start = s.find("{")
    end = s.rfind("}")
    if start < 0 or end < 0 or end <= start:
        raise ValueError("Evaluator did not print a JSON object to stdout.")
    return json.loads(s[start : end + 1])


def _apply_param_tokens(eval_cmd_template: str, params: Dict[str, float]) -> str:
    """
    Replace only the known Optuna parameter tokens: {B},{R},{D},{I},{A}.
    This avoids Python .format() interpreting other braces (e.g. JSON literals) as placeholders.
    """
    cmd = eval_cmd_template
    for k in ("B", "R", "D", "I", "A"):
        if k not in params:
            continue
        cmd = cmd.replace("{" + k + "}", str(params[k]))
    return cmd


def _run_eval(eval_cmd_template: str, params: Dict[str, float], timeout_s: int) -> Tuple[Dict[str, Any], str]:
    cmd = _apply_param_tokens(eval_cmd_template, params)
    argv = shlex.split(cmd)
    p = subprocess.run(
        argv,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        timeout=timeout_s if timeout_s > 0 else None,
        env=os.environ.copy(),
    )
    out = p.stdout or ""
    data = _parse_json_from_stdout(out)
    return data, out


def _score_constraint_first(penalty: float, efficiency: float) -> float:
    # Strongly prefer feasible (penalty == 0) points.
    # Within feasible region, maximize efficiency.
    if penalty <= 0:
        return 1_000_000.0 + float(efficiency)
    return -float(penalty)


def _score_weighted(penalty: float, efficiency: float, penalty_weight: float) -> float:
    # Single scalar: maximize efficiency - weight*penalty
    return float(efficiency) - penalty_weight * float(penalty)


def main() -> int:
    ap = argparse.ArgumentParser(description="Optuna driver for nozzle optimization (B,R,D,I,A).")
    ap.add_argument("--study-name", default="nozzle", help="Optuna study name.")
    ap.add_argument("--storage", default="sqlite:///optuna-nozzle.db", help="Optuna storage URL.")
    ap.add_argument("--trials", type=int, default=100, help="Number of trials to run.")
    ap.add_argument("--timeout-s", type=int, default=0, help="Per-trial evaluator timeout in seconds (0 = no timeout).")
    ap.add_argument("--eval-cmd", required=True, help="Evaluator command template, e.g. 'dotnet ... --B {B} --R {R} ...'.")
    ap.add_argument(
        "--objective",
        choices=["constraint-first", "weighted"],
        default="constraint-first",
        help="Scoring strategy.",
    )
    ap.add_argument("--penalty-weight", type=float, default=1e-3, help="Used only for weighted objective.")

    # Bounds (defaults are placeholders; you should pass real bounds)
    ap.add_argument("--B-low", type=float, required=True)
    ap.add_argument("--B-high", type=float, required=True)
    ap.add_argument("--B-step", type=float, default=0.0)

    ap.add_argument("--R-low", type=float, required=True)
    ap.add_argument("--R-high", type=float, required=True)
    ap.add_argument("--R-step", type=float, default=0.0)

    ap.add_argument("--D-low", type=float, required=True)
    ap.add_argument("--D-high", type=float, required=True)
    ap.add_argument("--D-step", type=float, default=1.0)

    ap.add_argument("--I-low", type=float, required=True)
    ap.add_argument("--I-high", type=float, required=True)
    ap.add_argument("--I-step", type=float, default=1.0)

    ap.add_argument("--A-low", type=float, required=True)
    ap.add_argument("--A-high", type=float, required=True)
    ap.add_argument("--A-step", type=float, default=1.0)

    args = ap.parse_args()

    specs = [
        ParamSpec("B", args.B_low, args.B_high, args.B_step if args.B_step > 0 else None, "float"),
        ParamSpec("R", args.R_low, args.R_high, args.R_step if args.R_step > 0 else None, "float"),
        ParamSpec("D", args.D_low, args.D_high, args.D_step if args.D_step > 0 else None, "int"),
        ParamSpec("I", args.I_low, args.I_high, args.I_step if args.I_step > 0 else None, "int"),
        ParamSpec("A", args.A_low, args.A_high, args.A_step if args.A_step > 0 else None, "int"),
    ]

    sampler = optuna.samplers.TPESampler(multivariate=True, seed=42)
    study = optuna.create_study(
        study_name=args.study_name,
        storage=args.storage,
        load_if_exists=True,
        direction="maximize",
        sampler=sampler,
    )

    def objective(trial: optuna.Trial) -> float:
        params = {s.name: s.suggest(trial) for s in specs}
        t0 = time.time()
        data, raw = _run_eval(args.eval_cmd, params, args.timeout_s)
        dt = time.time() - t0

        penalty = float(data.get("penalty", math.nan))
        efficiency = float(data.get("efficiency", math.nan))
        if math.isnan(penalty) or math.isnan(efficiency):
            raise ValueError(f"Evaluator JSON missing required keys. Got keys={list(data.keys())}")

        trial.set_user_attr("eval_seconds", dt)
        trial.set_user_attr("raw", raw[-4000:])  # keep last chunk for debugging
        for k in ("checkType", "powerKW"):
            if k in data:
                trial.set_user_attr(k, data[k])

        if args.objective == "constraint-first":
            return _score_constraint_first(penalty, efficiency)
        return _score_weighted(penalty, efficiency, args.penalty_weight)

    study.optimize(objective, n_trials=args.trials)

    best = study.best_trial
    print(json.dumps({"best_params": best.params, "best_value": best.value, "best_user_attrs": best.user_attrs}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

