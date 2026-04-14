## `head/` (Optuna driver)

This folder contains a **Python Optuna “outer loop”** that can drive your existing **.NET/Turba/DAT + penalty** evaluation loop.

Optuna proposes the 5 nozzle knobs **B, R, D, I, A**. For each trial it runs an external **evaluator command** (typically your .NET app) and expects the evaluator to print a **single JSON object** to stdout with (at minimum) `penalty` and `efficiency`.

### Evaluator contract (stdout JSON)

Your evaluator command must accept the 5 parameters (any CLI format you want) and print JSON like:

```json
{
  "penalty": 1234.5,
  "efficiency": 78.36,
  "powerKW": 3653,
  "checkType": "1190",
  "checks": { "Check_HOEHE": "TRUE", "Check_FMIN1": "FALSE" },
  "outputs": { "HOEHE": 8.0, "FMIN1": 722.9 }
}
```

Only `penalty` and `efficiency` are required by the Optuna driver; the rest is logged for debugging.

### Quick start

1. Create a Python venv and install requirements:

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r head/requirements.txt
```

2. Run Optuna with a command template that evaluates one point.

Example (you will replace `EVAL_CMD` with your real .NET command):

```bash
python head/run_optuna_nozzle.py \
  --study-name nozzle \
  --storage sqlite:///head/optuna-nozzle.db \
  --trials 200 \
  --eval-cmd "EVAL_CMD --B {B} --R {R} --D {D} --I {I} --A {A}"
```

### Notes

- The driver supports **step/grid** alignment via `--B-step`, `--R-step`, etc.
- Use `--objective constraint-first` to strongly prefer `penalty == 0` solutions before optimizing efficiency.
- Parallelization is possible, but **only** if your evaluator isolates files per trial (DAT/ERG working dirs).

