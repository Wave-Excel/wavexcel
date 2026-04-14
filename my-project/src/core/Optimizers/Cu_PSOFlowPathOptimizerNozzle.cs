using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HMBD.PSO_PenalityFunctionNozzle;
using Interfaces.ILogger;
using Microsoft.Extensions.Configuration;
using Models.LoadPointDataModel;
using Models.PreFeasibility;
using Models.TurbaOutputDataModel;
using Models.TurbineData;
using StartExecutionMain;
using Turba.Cu_TurbaConfig;
using HMBD.Cu_CW_Curve;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Text;
using System.Text.Json;

namespace Optimizers.PSOFlowPathNozzle
{
    public class RelationshipAwarePSOOptimizer
    {
        static int NumParticles = 10;
        private ILogger logger;
        private Random random = new Random(42);

        // PSO Arrays
        double[,] Position = new double[NumParticles, 5];
        double[,] Velocity = new double[NumParticles, 5];
        double[,] BestPosition = new double[NumParticles, 5];
        double[] BestFitness = new double[NumParticles];
        double[] GlobalBestPosition = new double[5];
        public static double GlobalBestFitness;
        public static double GlobalBestEfficiency;
        public static int mxlp = 0;
        double[] MinValues = new double[5];
        double[] MaxValues = new double[5];
        double[] Steps = new double[5];

        Dictionary<string, RelationshipInfo> relationships = new Dictionary<string, RelationshipInfo>();

        LoadPointDataModel loadPointDataModel;
        PreFeasibilityDataModel preFeasibilityDataModel;
        IConfiguration configuration;
        TurbineDataModel turbineDataModel;
        TurbaOutputModel turbaOutputModel;
        int psoIteration;
        static int IterationCounter;
        // Stores CSV data rows
        private List<string> csvRows = new List<string>();
        List<double> thrustValues = new List<double>();
        private string nozzleDatFilePath;



        public class RelationshipInfo
        {
            public string Parameter1 { get; set; }
            public string Parameter2 { get; set; }
            public RelationshipType Type { get; set; }
            public double Strength { get; set; }
            public string Description { get; set; }
        }

        public enum RelationshipType
        {
            StrongDirect,      // ↑↑↑ (3 up arrows)
            ModerateDirect,    // ↑↑ (2 up arrows)  
            WeakDirect,        // ↑ (1 up arrow)
            StrongInverse,     // ↓↓↓ (3 down arrows)
            ModerateInverse,   // ↓↓ (2 down arrows)
            WeakInverse,       // ↓ (1 down arrow)
            NoRelation         // - (dash)
        }
        private void CollectResultForCSV(
        double inletPressure,
        double inletTemperature,
        double massFlowRate,
        double exhaustPressure,
        double b, double r, double d, double i, double a,
        double efficiency, double power, double penalty,
        double hoehe, double deltaT, double wheelPressure, double wheelTemp,
        string lang, string checkPSI, double gbcLength, double fmin1,
        List<double> thrustValues)
            {   
            var sb = new StringBuilder();
            sb.Append($"{inletPressure},{inletTemperature},{massFlowRate},{exhaustPressure},");
            sb.Append($"{b},{r},{d},{i},{a},{efficiency},{power},{penalty},");
            sb.Append($"{hoehe},{deltaT},{wheelPressure},{wheelTemp},{lang},{checkPSI},{gbcLength},{fmin1}");

            // Append thrust values as separate columns
            for (int t = 0; t < thrustValues.Count; t++)
            {
                sb.Append($",{thrustValues[t]}");
            }

            csvRows.Add(sb.ToString());
        }





        public RelationshipAwarePSOOptimizer()
        {
            loadPointDataModel = LoadPointDataModel.getInstance();
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            psoIteration = configuration.GetValue<int>("AppSettings:PSO_Iteration");
            nozzleDatFilePath = configuration.GetValue<string>("AppSettings:NozzleDatFilePath");
            if (string.IsNullOrWhiteSpace(nozzleDatFilePath))
                nozzleDatFilePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
            preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
            turbineDataModel = TurbineDataModel.getInstance();
            turbaOutputModel = TurbaOutputModel.getInstance();
            logger = CustomExecutedClass.GlobalHost.Services.GetRequiredService<ILogger>();

            InitializeRelationshipsFromTable();
        }

        private void InitializeRelationshipsFromTable()
        {
            // Based on your relationship table - translating arrows to relationships

            // Admission Factor (BEAUFSCHL) relationships
            relationships["admission_nozzle_height"] = new RelationshipInfo
            {
                Parameter1 = "BEAUFSCHL",
                Parameter2 = "NozzleHeight",
                Type = RelationshipType.StrongInverse,
                Strength = 0.9,
                Description = "Admission Factor ↓↓↓ Nozzle Height"
            };

            relationships["admission_nozzle_area"] = new RelationshipInfo
            {
                Parameter1 = "BEAUFSCHL",
                Parameter2 = "NozzleArea",
                Type = RelationshipType.StrongDirect,
                Strength = 0.9,
                Description = "Admission Factor ↑↑↑ Nozzle Area"
            };

            // Wheel Chamber Pressure (RADKAMMER) relationships
            relationships["pressure_nozzle_height"] = new RelationshipInfo
            {
                Parameter1 = "RADKAMMER",
                Parameter2 = "NozzleHeight",
                Type = RelationshipType.WeakDirect,
                Strength = 0.3,
                Description = "Wheel Chamber Pressure ↑ Nozzle Height"
            };

            relationships["pressure_nozzle_area"] = new RelationshipInfo
            {
                Parameter1 = "RADKAMMER",
                Parameter2 = "NozzleArea",
                Type = RelationshipType.WeakDirect,
                Strength = 0.3,
                Description = "Wheel Chamber Pressure ↑ Nozzle Area"
            };

            relationships["pressure_wheel_temp"] = new RelationshipInfo
            {
                Parameter1 = "RADKAMMER",
                Parameter2 = "WheelChamberTemp",
                Type = RelationshipType.StrongDirect,
                Strength = 0.9,
                Description = "Wheel Chamber Pressure ↑↑↑ Wheel Chamber Temperature"
            };

            relationships["pressure_gbc_temp"] = new RelationshipInfo
            {
                Parameter1 = "RADKAMMER",
                Parameter2 = "GBCTempDiff",
                Type = RelationshipType.StrongDirect,
                Strength = 0.9,
                Description = "Wheel Chamber Pressure ↑↑↑ GBC Temperature Diff"
            };

            relationships["pressure_thrust"] = new RelationshipInfo
            {
                Parameter1 = "RADKAMMER",
                Parameter2 = "AxialThrust",
                Type = RelationshipType.WeakInverse,
                Strength = 0.3,
                Description = "Wheel Chamber Pressure ↓ Axial Thrust"
            };
            // todo
            relationships["pressure_blade_height"] = new RelationshipInfo
            {
                Parameter1 = "RADKAMMER",
                Parameter2 = "FirstBladeHeight",
                Type = RelationshipType.StrongInverse,
                Strength = 0.9,
                Description = "Wheel Chamber Pressure ↑↑ First Blade Height"
            };

            relationships["pressure_psi"] = new RelationshipInfo
            {
                Parameter1 = "RADKAMMER",
                Parameter2 = "PSI",
                Type = RelationshipType.StrongDirect,
                Strength = 0.9,
                Description = "Wheel Chamber Pressure ↑↑↑ PSI"
            };

            // Number of Stages relationships
            relationships["stages_gbc_length"] = new RelationshipInfo
            {
                Parameter1 = "DRUCKZIFFERN",
                Parameter2 = "GBCLength",
                Type = RelationshipType.StrongDirect,
                Strength = 0.9,
                Description = "Number of Stages ↑↑↑ GBC Length"
            };

            relationships["stages_gbc_temp"] = new RelationshipInfo
            {
                Parameter1 = "DRUCKZIFFERN",
                Parameter2 = "GBCTempDiff",
                Type = RelationshipType.WeakDirect,
                Strength = 0.3,
                Description = "Number of Stages ↑ GBC Temperature Diff"
            };

            relationships["stages_blade_height"] = new RelationshipInfo
            {
                Parameter1 = "DRUCKZIFFERN",
                Parameter2 = "FirstBladeHeight",
                Type = RelationshipType.WeakDirect,
                Strength = 0.3,
                Description = "Number of Stages ↑ First Blade Height"
            };

            relationships["stages_psi"] = new RelationshipInfo
            {
                Parameter1 = "DRUCKZIFFERN",
                Parameter2 = "PSI",
                Type = RelationshipType.StrongInverse,
                Strength = 0.9,
                Description = "Number of Stages ↓↓↓ PSI"
            };

            // Shaft Diameter (INNENDURCHMESSER) relationships
            relationships["shaft_thrust"] = new RelationshipInfo
            {
                Parameter1 = "INNENDURCHMESSER",
                Parameter2 = "AxialThrust",
                Type = RelationshipType.WeakDirect,
                Strength = 0.3,
                Description = "Shaft Diameter ↑ Axial Thrust (Direct Proportional)"
            };

            relationships["shaft_blade_height"] = new RelationshipInfo
            {
                Parameter1 = "INNENDURCHMESSER",
                Parameter2 = "FirstBladeHeight",
                Type = RelationshipType.ModerateDirect,
                Strength = 0.6,
                Description = "Shaft Diameter ↑↑  First Blade Height"
            };

            relationships["shaft_psi"] = new RelationshipInfo
            {
                Parameter1 = "INNENDURCHMESSER",
                Parameter2 = "PSI",
                Type = RelationshipType.StrongInverse,
                Strength = 0.9,
                Description = "Shaft Diameter ↓↓↓ PSI"
            };

            // Balance Piston Diameter (AUSGLEICHSKOLBEN) relationships
            relationships["piston_thrust"] = new RelationshipInfo
            {
                Parameter1 = "AUSGLEICHSKOLBEN",
                Parameter2 = "AxialThrust",
                Type = RelationshipType.StrongInverse,
                Strength = 0.9,
                Description = "Balance Piston Diameter ↓↓↓ Axial Thrust (Inverse Proportional)"
            };

            Logger($"Initialized {relationships.Count} parameter relationships from table");
        }

        public void InvokeTurbineDesigner()
        {
            bool useOllama = configuration.GetValue<bool>("AppSettings:UseOllamaGuidedNozzle");
            if (useOllama)
            {
                Logger("Starting Ollama-guided nozzle DAT optimization (local LLM proposes next B,R,D,I,A; Turba + penalty are authoritative).");
                InvokeOllamaGuidedOptimizer();
                Logger("___________________________________________________");
                return;
            }

            Logger("Starting Relationship-Aware PSO Optimization");
            Logger("Relationships loaded from engineering table:");
            foreach (var rel in relationships.Values)
            {
                Logger($"  {rel.Description}");
            }
            PSOLoop();
            Logger("___________________________________________________");
        }

        public void PSOLoop()
        {
            int iterations = 3;
            InitializeParticles();

            for (int iter = 1; iter <= iterations; iter++)
            {
                Logger($"################ ITERATION: {iter} ##################");
                IterationCounter = iter;

                EvaluateAndUpdateParticles();
                UpdateParticlesWithRelationships();

                Logger($"Global Best Fitness: {GlobalBestFitness:F6}");
                Logger($"Global Best Efficiency: {GlobalBestEfficiency:F6}%");

                // Only accept solutions with penalty = 0
                if (GlobalBestFitness > 0)
                {
                    Logger($"*** FEASIBLE SOLUTION FOUND AT ITERATION {iter} ***");
                    if (GlobalBestEfficiency > 85)
                    {
                        Logger($"*** HIGH EFFICIENCY ACHIEVED - CONVERGENCE ***");
                        break;
                    }
                }

                // Relationship-based adaptive adjustments
                if (iter % 15 == 0)
                {
                    AnalyzeRelationshipPerformance();
                    ApplyRelationshipGuidedDiversification();
                }
            }

            FinalEvaluation();
        }

        private void InitializeParameterBounds()
        {
            double inletPressure = preFeasibilityDataModel.InletPressureActualValue;
            double backPressure = preFeasibilityDataModel.BackpressureActualValue;

            MinValues[0] = 0.14; MaxValues[0] = 0.78; Steps[0] = 0.03;
            MinValues[1] = Math.Max(15, backPressure + 5); MaxValues[1] = 0.8 * inletPressure; Steps[1] = 4;
            MinValues[2] = -11; MaxValues[2] = -6; Steps[2] = 1;
            MinValues[3] = 200; MaxValues[3] = 230; Steps[3] = 5;
            MinValues[4] = 240; MaxValues[4] = 270; Steps[4] = 5;
        }

        public void InitializeParticles()
        {
            InitializeParameterBounds();

            // Initialize particles with relationship-guided positioning
            for (int i = 0; i < NumParticles; i++)
            {
                InitializeParticleWithRelationshipGuidance(i);
                BestFitness[i] = double.MinValue;
            }

            GlobalBestFitness = double.MinValue;
            Logger($"Initialized {NumParticles} particles with conservative relationship guidance");
        }

        private void InitializeParticleWithRelationshipGuidance(int particleIndex)
        {
            // Conservative initialization to avoid constraint violations

            // Start with lower admission factor to control nozzle area
            double admissionFactor = MinValues[0] + random.NextDouble() * (MaxValues[0] - MinValues[0]);
            Position[particleIndex, 0] = admissionFactor; // Cap at safe level

            // Conservative wheel chamber pressure
            double wheelPressure = MinValues[1] + random.NextDouble() * (MaxValues[1] - MinValues[1]);
            Position[particleIndex, 1] = wheelPressure; // Cap at safe level

            // Conservative number of stages
            Position[particleIndex, 2] = MinValues[2] + random.NextDouble() * (MaxValues[2] - MinValues[2]);

            // Thrust optimization relationships
            double thrustOptimizationFactor = random.NextDouble();

            // Prefer smaller shaft diameter for lower thrust
            Position[particleIndex, 3] = MinValues[3] + (MaxValues[3] - MinValues[3]) * thrustOptimizationFactor;

            // Prefer larger piston diameter for thrust compensation
            Position[particleIndex, 4] = MinValues[4] + thrustOptimizationFactor * (MaxValues[4] - MinValues[4]);

            // Ensure bounds and discretization
            for (int j = 0; j < 5; j++)
            {
                Position[particleIndex, j] = Math.Max(MinValues[j], Math.Min(MaxValues[j], Position[particleIndex, j]));
                Position[particleIndex, j] = Math.Round(Position[particleIndex, j] / Steps[j]) * Steps[j];
                Velocity[particleIndex, j] = 0;
                BestPosition[particleIndex, j] = Position[particleIndex, j];
            }
        }

        private void ApplyConstraintSpecificCorrection(int particleIndex, double penaltyScore)
        {
            Logger($"=== CONSTRAINT-SPECIFIC CORRECTION FOR PARTICLE {particleIndex + 1} ===");
            Logger($"Current Parameters: B={Position[particleIndex, 0]:F3}, R={Position[particleIndex, 1]:F1}, D={Position[particleIndex, 2]:F0}, I={Position[particleIndex, 3]:F0}, A={Position[particleIndex, 4]:F0}");

            // **DETERMINE BCD TYPE FROM PREFEASIBILITY**
            bool G14 = (preFeasibilityDataModel.Decision == "TRUE");
            bool G26 = (preFeasibilityDataModel.Decision_2 == "TRUE");
            string BCD = "";
            string checkType = "";

            if (G14)
            {
                BCD = "1120";
                checkType = "1120";
            }
            else if (!G14 && G26)
            {
                BCD = "1190";
                checkType = "1190";
            }

            Logger($"Detected BCD Type: {checkType}");

            // Get current output values
            double currentNozzleArea = turbaOutputModel.OutputDataList[0].FMIN1;
            double currentWheelTemp = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature;
            double currentDeltaT = turbaOutputModel.OutputDataList[0].DELTA_T;
            double currentGBCLength = turbaOutputModel.OutputDataList[0].GBC_Length;
            double currentNozzleHeight = turbaOutputModel.OutputDataList[0].HOEHE;

            Logger($"Current Outputs: NozzleArea={currentNozzleArea:F1}, WheelTemp={currentWheelTemp:F1}, DeltaT={currentDeltaT:F1}, GBCLength={currentGBCLength:F1}, NozzleHeight={currentNozzleHeight:F1}");

            // **BCD-SPECIFIC CONSTRAINT CORRECTIONS**
            if (checkType == "1120")
            {
                ApplyBCD1120Corrections(particleIndex, currentNozzleArea, currentWheelTemp, currentDeltaT, currentGBCLength, currentNozzleHeight);
            }
            else if (checkType == "1190")
            {
                ApplyBCD1190Corrections(particleIndex, currentNozzleArea, currentWheelTemp, currentDeltaT, currentGBCLength, currentNozzleHeight);
            }

            Logger($"Corrected Parameters: B={Position[particleIndex, 0]:F3}, R={Position[particleIndex, 1]:F1}, D={Position[particleIndex, 2]:F0}, I={Position[particleIndex, 3]:F0}, A={Position[particleIndex, 4]:F0}");
            Logger("=== END CONSTRAINT CORRECTION ===");

        }
        private double WheelChamberTempCurveWithCW1GetUpperLimit(double wheelchamberP)
        {
            // You already have this in PenaltyScoreCalculator - reuse the logic
            return WheelChamber.WheelChamberTemp_CurveWithCW1_GetUpperLimit(wheelchamberP);
        }

        private double WheelChamberTempCurveWithCW2GetUpperLimit(double wheelchamberP)
        {
            // You already have this in PenaltyScoreCalculator - reuse the logic  
            return WheelChamber.WheelChamberTemp_CurveWithCW2_GetUpperLimit(wheelchamberP);
        }

        private void ApplyBCD1190Corrections(int particleIndex, double currentNozzleArea, double currentWheelTemp, double currentDeltaT, double currentGBCLength, double currentNozzleHeight)
        {
            Logger("=== APPLYING BCD 1190 CORRECTIONS ===");
            double inletTemperature = preFeasibilityDataModel.TemperatureActualValue;
            double wheelchamberP = turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure;
            double wheelchamberTempLimit;

            if (inletTemperature <= 500)
            {
                wheelchamberTempLimit = WheelChamberTempCurveWithCW1GetUpperLimit(wheelchamberP);
            }
            else
            {
                wheelchamberTempLimit = WheelChamberTempCurveWithCW2GetUpperLimit(wheelchamberP);
            }

            // **1. NOZZLE AREA CORRECTION (Limit: 1400 - same as 1120)**
            if (currentNozzleArea > 1400 || currentNozzleArea <= 0)
            {
                double excessArea = Math.Abs(currentNozzleArea - 1400); // ← CORRECT VARIABLE
                double correctionFactor = Math.Min(0.15, excessArea / 1400.0);

                double oldAdmission = Position[particleIndex, 0]; // ← CORRECT PARAMETER
                Position[particleIndex, 0] = Math.Max(MinValues[0], Position[particleIndex, 0] - correctionFactor);
                Position[particleIndex, 0] = Math.Round(Position[particleIndex, 0] / Steps[0]) * Steps[0];
                Logger($"BCD1190 NOZZLE AREA CORRECTION: {currentNozzleArea:F1} > 1400, Reduced Admission: {oldAdmission:F3} → {Position[particleIndex, 0]:F3}");

            }

            // **2. WHEEL CHAMBER TEMPERATURE CORRECTION (Dynamic limit based on CW curves for BCD1190)**

            if (currentWheelTemp > wheelchamberTempLimit || currentWheelTemp <= 0)
            {
                double excessTemp = Math.Abs(currentWheelTemp - wheelchamberTempLimit);
                double correctionFactor = Math.Min(3.0, excessTemp / 20.0);

                double oldPressure = Position[particleIndex, 1];
                Position[particleIndex, 1] = Math.Max(MinValues[1], Position[particleIndex, 1] - correctionFactor);
                Position[particleIndex, 1] = Math.Round(Position[particleIndex, 1] / Steps[1]) * Steps[1];

                Logger($"BCD1190 WHEEL TEMP CORRECTION: {currentWheelTemp:F1}°C > {wheelchamberTempLimit:F1}°C, Reduced Pressure: {oldPressure:F1} → {Position[particleIndex, 1]:F1}");
            }

            // **3. DELTA T CORRECTION (Limit: 210 for BCD1190)**
            if (currentDeltaT > 210 || currentDeltaT <= 0)
            {
                double excessDeltaT = Math.Abs(currentDeltaT - 210);
                double correctionFactor = Math.Min(2.0, excessDeltaT / 25.0);

                double oldPressure = Position[particleIndex, 1];
                Position[particleIndex, 1] = Math.Max(MinValues[1], Position[particleIndex, 1] - correctionFactor);
                Position[particleIndex, 1] = Math.Round(Position[particleIndex, 1] / Steps[1]) * Steps[1];

                Logger($"BCD1190 DELTA T CORRECTION: {currentDeltaT:F1} > 210, Reduced Pressure: {oldPressure:F1} → {Position[particleIndex, 1]:F1}");
            }

            // **4. GBC LENGTH CORRECTION (Limit: 356 or 371 based on varicode7 for BCD1190)**
            bool varicode7 = false; // You may need to get this from your configuration
            double GBCLengthLimit = varicode7 ? 356 : 371;

            if (currentGBCLength > GBCLengthLimit || currentGBCLength <= 0)
            {
                double oldStages = Position[particleIndex, 2];
                Position[particleIndex, 2] = Math.Max(MinValues[2], Position[particleIndex, 2] - 1);
                Position[particleIndex, 2] = Math.Round(Position[particleIndex, 2] / Steps[2]) * Steps[2];

                Logger($"BCD1190 GBC LENGTH CORRECTION: {currentGBCLength:F1} > {GBCLengthLimit}, Reduced Stages: {oldStages:F0} → {Position[particleIndex, 2]:F0}");
            }

            // **5. NOZZLE HEIGHT CORRECTION (Range: 10.5-27 for BCD1190 - same as 1120)**
            if (currentNozzleHeight < 10.5 || currentNozzleHeight > 27)
            {
                if (currentNozzleHeight < 10.5)
                {
                    double oldAdmission = Position[particleIndex, 0];
                    Position[particleIndex, 0] = Math.Min(MaxValues[0], Position[particleIndex, 0] - 0.05);
                    Position[particleIndex, 0] = Math.Round(Position[particleIndex, 0] / Steps[0]) * Steps[0];
                    Logger($"BCD1190 NOZZLE HEIGHT LOW: {currentNozzleHeight:F1} < 10.5, Decreased  Admission: {oldAdmission:F3} → {Position[particleIndex, 0]:F3}");
                }
                else
                {
                    double oldAdmission = Position[particleIndex, 0];
                    Position[particleIndex, 0] = Math.Max(MinValues[0], Position[particleIndex, 0] + 0.05);
                    Position[particleIndex, 0] = Math.Round(Position[particleIndex, 0] / Steps[0]) * Steps[0];
                    Logger($"BCD1190 NOZZLE HEIGHT HIGH: {currentNozzleHeight:F1} > 27, Increased Admission: {oldAdmission:F3} → {Position[particleIndex, 0]:F3}");
                }
            }
            if (turbaOutputModel.Check_PSI == "FALSE")
            {
                string psiFailureInfo = turbaOutputModel.OutputDataList[0].PSI; // Contains "LP" + loadPointCounter or "FALSE"
                Logger($"PSI Failure Details: {psiFailureInfo} (PSI < 1.8 detected)");

                // ONLY correction: Number of Stages ↓↓↓ PSI (Strong Inverse Relationship)
                // Reducing stages is the most direct way to improve PSI values
                double oldStages = Position[particleIndex, 2];
                Position[particleIndex, 2] = Math.Max(MinValues[2], Position[particleIndex, 2] + 2);
                Position[particleIndex, 2] = Math.Round(Position[particleIndex, 2] / Steps[2]) * Steps[2];

                Logger($"PSI CORRECTION - Stages ONLY: Reduced from {oldStages:F0} to {Position[particleIndex, 2]:F0} (fewer stages → PSI ≥ 1.8)");

                // Do NOT modify other parameters when PSI fails
                Logger($"Other parameters unchanged: B={Position[particleIndex, 0]:F3}, R={Position[particleIndex, 1]:F1}, I={Position[particleIndex, 3]:F0}, A={Position[particleIndex, 4]:F0}");
            }
            //Logger($"PSI CORRECTION - Stages: {Math.Abs(oldStages):F0} → {Math.Abs(Position[particleIndex, 2]):F0} stages (fewer stages → PSI ≥ 1.8)");
            if (turbaOutputModel.Check_Lang == "FALSE")
            {
                Logger($"LANG CHECK FAILED - Applying correction");

                // LANG is related to blade geometry - adjust stages and shaft diameter
                double oldStages = Position[particleIndex, 2];
                Position[particleIndex, 2] = Math.Max(MinValues[2], Position[particleIndex, 2] - 1);
                Position[particleIndex, 2] = Math.Round(Position[particleIndex, 2] / Steps[2]) * Steps[2];

                Logger($"LANG CORRECTION - Stages: Reduced from {oldStages:F0} to {Position[particleIndex, 2]:F0}");
            }
            // **THRUST CONSTRAINT CORRECTION - SHAFT DIAMETER ONLY**
            if (turbaOutputModel.Check_Thrust == "FALSE")
            {
                Logger($"THRUST CHECK FAILED - Applying shaft-only correction");

                // Analyze actual thrust values to determine correction direction
                List<OutputLoadPoint> lplist = turbaOutputModel.OutputDataList;
                double maxThrust = double.MinValue;
                double minThrust = double.MaxValue;

                for (int i = 0; i < 10; i++)
                {
                    double thrust = lplist[i].Thrust;
                    maxThrust = Math.Max(maxThrust, thrust);
                    minThrust = Math.Min(minThrust, thrust);

                    if (thrust > 0.8 || thrust < -0.8)
                    {
                        Logger($"Load Point {i + 1}: Thrust = {thrust:F3} (VIOLATION)");
                    }
                }

                Logger($"Thrust Analysis: Min={minThrust:F3}, Max={maxThrust:F3}");

                // **SINGLE PARAMETER CORRECTION: SHAFT DIAMETER ONLY**
                if (maxThrust > 0.8) // Thrust too HIGH
                {
                    Logger($"THRUST TOO HIGH: {maxThrust:F3} > 0.8 - REDUCING shaft diameter");

                    double oldShaft = Position[particleIndex, 3];
                    Position[particleIndex, 3] = Math.Max(MinValues[3], Position[particleIndex, 3] - 5);
                    Position[particleIndex, 3] = Math.Round(Position[particleIndex, 3] / Steps[3]) * Steps[3];

                    Logger($"THRUST CORRECTION - Shaft ONLY: {oldShaft:F0} → {Position[particleIndex, 3]:F0} (smaller shaft → lower thrust)");
                }
                else if (minThrust < -0.8) // Thrust too LOW
                {
                    Logger($"THRUST TOO LOW: {minThrust:F3} < -0.8 - INCREASING shaft diameter");

                    double oldShaft = Position[particleIndex, 3];
                    Position[particleIndex, 3] = Math.Min(MaxValues[3], Position[particleIndex, 3] + 5);
                    Position[particleIndex, 3] = Math.Round(Position[particleIndex, 3] / Steps[3]) * Steps[3];

                    Logger($"THRUST CORRECTION - Shaft ONLY: {oldShaft:F0} → {Position[particleIndex, 3]:F0} (larger shaft → higher thrust)");
                }

                // **ALL OTHER PARAMETERS REMAIN UNCHANGED**
                Logger($"Other parameters UNCHANGED: B={Position[particleIndex, 0]:F3}, R={Position[particleIndex, 1]:F1}, D={Position[particleIndex, 2]:F0}, A={Position[particleIndex, 4]:F0}");
            }

        }

        private void ApplyBCD1120Corrections(int particleIndex, double currentNozzleArea, double currentWheelTemp, double currentDeltaT, double currentGBCLength, double currentNozzleHeight)
        {
            Logger("=== APPLYING BCD 1120 CORRECTIONS ===");

            // **1. NOZZLE AREA CORRECTION (Limit: 1400)**
            if (currentNozzleArea > 1400 || currentNozzleArea <= 0)
            {
                double excessArea = Math.Abs(currentNozzleArea - 1400);
                double correctionFactor = Math.Min(0.15, excessArea / 1400.0);

                double oldAdmission = Position[particleIndex, 0];
                Position[particleIndex, 0] = Math.Max(MinValues[0], Position[particleIndex, 0] - correctionFactor);
                Position[particleIndex, 0] = Math.Round(Position[particleIndex, 0] / Steps[0]) * Steps[0];

                Logger($"BCD1120 NOZZLE AREA CORRECTION: {currentNozzleArea:F1} > 1400, Reduced Admission: {oldAdmission:F3} → {Position[particleIndex, 0]:F3}");
            }

            // **2. WHEEL CHAMBER TEMPERATURE CORRECTION (Limit: 410°C for BCD1120)**
            if (currentWheelTemp > 410 || currentWheelTemp <= 0)
            {
                double excessTemp = Math.Abs(currentWheelTemp - 410);
                double correctionFactor = Math.Min(3.0, excessTemp / 20.0);

                double oldPressure = Position[particleIndex, 1];
                Position[particleIndex, 1] = Math.Max(MinValues[1], Position[particleIndex, 1] - correctionFactor);
                Position[particleIndex, 1] = Math.Round(Position[particleIndex, 1] / Steps[1]) * Steps[1];

                Logger($"BCD1120 WHEEL TEMP CORRECTION: {currentWheelTemp:F1}°C > 410°C, Reduced Pressure: {oldPressure:F1} → {Position[particleIndex, 1]:F1}");
            }

            // **3. DELTA T CORRECTION (Limit: 240 for BCD1120)**
            if (currentDeltaT > 240 || currentDeltaT <= 0)
            {
                double excessDeltaT = Math.Abs(currentDeltaT - 240);
                double correctionFactor = Math.Min(2.0, excessDeltaT / 30.0);

                double oldPressure = Position[particleIndex, 1];
                Position[particleIndex, 1] = Math.Max(MinValues[1], Position[particleIndex, 1] - correctionFactor);
                Position[particleIndex, 1] = Math.Round(Position[particleIndex, 1] / Steps[1]) * Steps[1];

                Logger($"BCD1120 DELTA T CORRECTION: {currentDeltaT:F1} > 240, Reduced Pressure: {oldPressure:F1} → {Position[particleIndex, 1]:F1}");
            }

            // **4. GBC LENGTH CORRECTION (Limit: 285 for BCD1120)**
            if (currentGBCLength > 285 || currentGBCLength <= 0)
            {
                double oldStages = Position[particleIndex, 2];
                Position[particleIndex, 2] = Math.Max(MinValues[2], Position[particleIndex, 2] - 1);
                Position[particleIndex, 2] = Math.Round(Position[particleIndex, 2] / Steps[2]) * Steps[2];

                Logger($"BCD1120 GBC LENGTH CORRECTION: {currentGBCLength:F1} > 285, Reduced Stages: {oldStages:F0} → {Position[particleIndex, 2]:F0}");
            }

            // **5. NOZZLE HEIGHT CORRECTION (Range: 10.5-27 for BCD1120)**
            if (currentNozzleHeight < 10.5 || currentNozzleHeight > 27)
            {
                if (currentNozzleHeight < 10.5)
                {
                    // Increase admission factor to increase nozzle height (inverse relationship ↓↓↓)
                    double oldAdmission = Position[particleIndex, 0];
                    Position[particleIndex, 0] = Math.Min(MaxValues[0], Position[particleIndex, 0] - 0.05);
                    Position[particleIndex, 0] = Math.Round(Position[particleIndex, 0] / Steps[0]) * Steps[0];
                    Logger($"BCD1120 NOZZLE HEIGHT LOW: {currentNozzleHeight:F1} < 10.5, Decreased Admission: {oldAdmission:F3} → {Position[particleIndex, 0]:F3}");
                }
                else
                {
                    // Decrease admission factor to decrease nozzle height
                    double oldAdmission = Position[particleIndex, 0];
                    Position[particleIndex, 0] = Math.Max(MinValues[0], Position[particleIndex, 0] + 0.05);
                    Position[particleIndex, 0] = Math.Round(Position[particleIndex, 0] / Steps[0]) * Steps[0];
                    Logger($"BCD1120 NOZZLE HEIGHT HIGH: {currentNozzleHeight:F1} > 27, Increased Admission: {oldAdmission:F3} → {Position[particleIndex, 0]:F3}");
                }
            }
            if (turbaOutputModel.Check_PSI == "FALSE")
            {
                string psiFailureInfo = turbaOutputModel.OutputDataList[0].PSI; // Contains "LP" + loadPointCounter or "FALSE"
                Logger($"PSI Failure Details: {psiFailureInfo} (PSI < 1.8 detected)");

                // ONLY correction: Number of Stages ↓↓↓ PSI (Strong Inverse Relationship)
                // Reducing stages is the most direct way to improve PSI values
                double oldStages = Position[particleIndex, 2];
                Position[particleIndex, 2] = Math.Max(MinValues[2], Position[particleIndex, 2] + 2);
                Position[particleIndex, 2] = Math.Round(Position[particleIndex, 2] / Steps[2]) * Steps[2];

                Logger($"PSI CORRECTION - Stages ONLY: Reduced from {oldStages:F0} to {Position[particleIndex, 2]:F0} (fewer stages → PSI ≥ 1.8)");

                // Do NOT modify other parameters when PSI fails
                Logger($"Other parameters unchanged: B={Position[particleIndex, 0]:F3}, R={Position[particleIndex, 1]:F1}, I={Position[particleIndex, 3]:F0}, A={Position[particleIndex, 4]:F0}");
            }
            if (turbaOutputModel.Check_Lang == "FALSE")
            {
                Logger($"LANG CHECK FAILED - Applying correction");

                // LANG is related to blade geometry - adjust stages and shaft diameter
                double oldStages = Position[particleIndex, 2];
                Position[particleIndex, 2] = Math.Max(MinValues[2], Position[particleIndex, 2] - 1);
                Position[particleIndex, 2] = Math.Round(Position[particleIndex, 2] / Steps[2]) * Steps[2];

                Logger($"LANG CORRECTION - Stages: Reduced from {oldStages:F0} to {Position[particleIndex, 2]:F0}");
            }
            // **THRUST CONSTRAINT CORRECTION - SHAFT DIAMETER ONLY**
            if (turbaOutputModel.Check_Thrust == "FALSE")
            {
                Logger($"THRUST CHECK FAILED - Applying shaft-only correction");

                // Analyze actual thrust values to determine correction direction
                List<OutputLoadPoint> lplist = turbaOutputModel.OutputDataList;
                double maxThrust = double.MinValue;
                double minThrust = double.MaxValue;

                for (int i = 0; i < 10; i++)
                {
                    double thrust = lplist[i].Thrust;
                    maxThrust = Math.Max(maxThrust, thrust);
                    minThrust = Math.Min(minThrust, thrust);

                    if (thrust > 0.8 || thrust < -0.8)
                    {
                        Logger($"Load Point {i + 1}: Thrust = {thrust:F3} (VIOLATION)");
                    }
                }

                Logger($"Thrust Analysis: Min={minThrust:F3}, Max={maxThrust:F3}");

                // **SINGLE PARAMETER CORRECTION: SHAFT DIAMETER ONLY**
                if (maxThrust > 0.8) // Thrust too HIGH
                {
                    Logger($"THRUST TOO HIGH: {maxThrust:F3} > 0.8 - REDUCING shaft diameter");

                    double oldShaft = Position[particleIndex, 3];
                    Position[particleIndex, 3] = Math.Max(MinValues[3], Position[particleIndex, 3] - 5);
                    Position[particleIndex, 3] = Math.Round(Position[particleIndex, 3] / Steps[3]) * Steps[3];

                    Logger($"THRUST CORRECTION - Shaft ONLY: {oldShaft:F0} → {Position[particleIndex, 3]:F0} (smaller shaft → lower thrust)");
                }
                else if (minThrust < -0.8) // Thrust too LOW
                {
                    Logger($"THRUST TOO LOW: {minThrust:F3} < -0.8 - INCREASING shaft diameter");

                    double oldShaft = Position[particleIndex, 3];
                    Position[particleIndex, 3] = Math.Min(MaxValues[3], Position[particleIndex, 3] + 5);
                    Position[particleIndex, 3] = Math.Round(Position[particleIndex, 3] / Steps[3]) * Steps[3];

                    Logger($"THRUST CORRECTION - Shaft ONLY: {oldShaft:F0} → {Position[particleIndex, 3]:F0} (larger shaft → higher thrust)");
                }

                // **ALL OTHER PARAMETERS REMAIN UNCHANGED**
                Logger($"Other parameters UNCHANGED: B={Position[particleIndex, 0]:F3}, R={Position[particleIndex, 1]:F1}, D={Position[particleIndex, 2]:F0}, A={Position[particleIndex, 4]:F0}");
            }

        }


        public void UpdateParticlesWithRelationships()
        {
            double w = 0.4; // Inertia weight
            double c1 = 2.0; // Cognitive coefficient
            double c2 = 2.0; // Social coefficient

            for (int i = 0; i < NumParticles; i++)
            {
                for (int j = 0; j < 5; j++)
                {


                    
                    double r1 = random.NextDouble();
                    double r2 = random.NextDouble();

                    // Standard PSO velocity update
                    Velocity[i, j] = w * Velocity[i, j] +
                                    c1 * r1 * (BestPosition[i, j] - Position[i, j]) +
                                    c2 * r2 * (GlobalBestPosition[j] - Position[i, j]);

                    // Apply relationship-based velocity modifications
                    Velocity[i, j] += CalculateRelationshipInfluence(i, j);

                    if (j == 2)
                    {
                        // Update position
                        Position[i, j] -= Velocity[i, j];

                        // Boundary constraints
                        Position[i, j] = Math.Max(MinValues[j], Math.Min(MaxValues[j], Position[i, j]));
                        Position[i, j] = Math.Round(Position[i, j] / Steps[j]) * Steps[j];
                    }
                    else
                    {
                        Position[i, j] += Velocity[i, j];

                        // Boundary constraints
                        Position[i, j] = Math.Max(MinValues[j], Math.Min(MaxValues[j], Position[i, j]));
                        Position[i, j] = Math.Round(Position[i, j] / Steps[j]) * Steps[j];

                    }

                    double maxVel = (MaxValues[j] - MinValues[j]) * 0.1;
                    Velocity[i, j] = Math.Max(-maxVel, Math.Min(maxVel, Velocity[i, j]));
                }

                // Apply relationship constraints after position update
                ApplyRelationshipConstraints(i);
            }
        }

        private double CalculateRelationshipInfluence(int particleIndex, int parameterIndex)
        {
            double influence = 0.0;
            string[] paramNames = { "BEAUFSCHL", "RADKAMMER", "DRUCKZIFFERN", "INNENDURCHMESSER", "AUSGLEICHSKOLBEN" };
            string currentParam = paramNames[parameterIndex];

            foreach (var relationship in relationships.Values)
            {
                if (relationship.Parameter1 == currentParam)
                {
                    influence += CalculateSpecificInfluence(particleIndex, parameterIndex, relationship);
                }
            }

            return influence * 0.05; // Reduced influence to prevent overshooting
        }

        private double CalculateSpecificInfluence(int particleIndex, int parameterIndex, RelationshipInfo relationship)
        {
            double influence = 0.0;
            double strengthMultiplier = GetStrengthMultiplier(relationship.Type);

            switch (relationship.Type)
            {
                case RelationshipType.StrongDirect:
                case RelationshipType.ModerateDirect:
                case RelationshipType.WeakDirect:
                    // Direct relationship - encourage positive movement
                    influence = random.NextDouble() * strengthMultiplier * Steps[parameterIndex];
                    break;

                case RelationshipType.StrongInverse:
                case RelationshipType.ModerateInverse:
                case RelationshipType.WeakInverse:
                    // Inverse relationship - encourage negative movement
                    influence = -random.NextDouble() * strengthMultiplier * Steps[parameterIndex];
                    break;

                case RelationshipType.NoRelation:
                    influence = 0.0;
                    break;
            }

            return influence;
        }

        private double GetStrengthMultiplier(RelationshipType type)
        {
            switch (type)
            {
                case RelationshipType.StrongDirect:
                case RelationshipType.StrongInverse:
                    return 0.9; // ↑↑↑ or ↓↓↓
                case RelationshipType.ModerateDirect:
                case RelationshipType.ModerateInverse:
                    return 0.6; // ↑↑ or ↓↓
                case RelationshipType.WeakDirect:
                case RelationshipType.WeakInverse:
                    return 0.3; // ↑ or ↓
                default:
                    return 0.0;
            }
        }

        private void ApplyRelationshipConstraints(int particleIndex)
        {
            double shaftNormalized = (Position[particleIndex, 3] - MinValues[3]) / (MaxValues[3] - MinValues[3]);
            double targetPistonNormalized = 1.0 - shaftNormalized * 0.7; // Inverse relationship

            double adjustedPiston = MinValues[4] + targetPistonNormalized * (MaxValues[4] - MinValues[4]);
            Position[particleIndex, 4] = Math.Max(MinValues[4], Math.Min(MaxValues[4], adjustedPiston));
            Position[particleIndex, 4] = Math.Round(Position[particleIndex, 4] / Steps[4]) * Steps[4];
        }

        public void EvaluateAndUpdateParticles()
        {
            Logger("=== PARTICLE EVALUATION WITH DETAILED PARAMETER TRACKING ===");
            int feasibleCount = 0;
            int blacklistedCount = 0;
            int correctedCount = 0;

            for (int i = 0; i < NumParticles; i++)
            {
                try
                {
                    Logger($"--- EVALUATING PARTICLE {i + 1} ---");
                    Logger($"Input Parameters: B={Position[i, 0]:F3}, R={Position[i, 1]:F1}, D={Position[i, 2]:F0}, I={Position[i, 3]:F0}, A={Position[i, 4]:F0}");

                    // **BLACKLIST CHECK**
                    if (IsBlacklistedCombination(i))
                    {
                        Logger($"Particle {i + 1}: BLACKLISTED - Redirecting to safe parameters");
                        blacklistedCount++;
                        InitializeParticleWithRelationshipGuidance(i);
                        Logger($"Redirected to: B={Position[i, 0]:F3}, R={Position[i, 1]:F1}, D={Position[i, 2]:F0}, I={Position[i, 3]:F0}, A={Position[i, 4]:F0}");
                        // Don't continue - evaluate the redirected parameters
                    }

                    // **INITIAL SIMULATION RUN**
                    RunBlackboxApplication(Position[i, 0], Position[i, 1], Position[i, 2], Position[i, 3], Position[i, 4]);
                    thrustValues.Clear();
                    for (int lp = 0; lp < mxlp; lp++)
                    {
                        thrustValues.Add(turbaOutputModel.OutputDataList[lp].Thrust);
                    }

                    double currentEfficiency = turbaOutputModel.OutputDataList[0].Efficiency;
                    double currentPenaltyScore = GetPenaltyScore();
                    double currentPower = turbaOutputModel.OutputDataList[0].Power_KW;
                    CollectResultForCSV(
                        turbineDataModel.InletPressure,
                        turbineDataModel.InletTemperature,
                        turbineDataModel.MassFlowRate,
                        turbineDataModel.ExhaustPressure,
                        Position[i, 0],
                        Position[i, 1],
                        Position[i, 2],
                        Position[i, 3],
                        Position[i, 4],
                        currentEfficiency,
                        currentPower,
                        currentPenaltyScore,
                        turbaOutputModel.OutputDataList[0].HOEHE,
                        turbaOutputModel.OutputDataList[0].DELTA_T,
                        turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure,
                        turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature,
                        turbaOutputModel.OutputDataList[0].Lang,
                        turbaOutputModel.Check_PSI,
                        turbaOutputModel.OutputDataList[0].GBC_Length,
                        turbaOutputModel.OutputDataList[0].FMIN1,
                        thrustValues
                    );

                    // **CHECK IF CORRECTION IS NEEDED**
                    if (currentPenaltyScore > 0)
                    {
                        Logger($"Particle {i + 1}: CONSTRAINT VIOLATION - Penalty: {currentPenaltyScore:F2}");

                        // Store original parameters for comparison
                        double[] originalParams = new double[5];
                        for (int j = 0; j < 5; j++)
                        {
                            originalParams[j] = Position[i, j];
                        }

                        // Apply constraint-specific correction
                        ApplyConstraintSpecificCorrection(i, currentPenaltyScore);

                        // Check if parameters actually changed
                        bool parametersChanged = false;
                        for (int j = 0; j < 5; j++)
                        {
                            if (Math.Abs(Position[i, j] - originalParams[j]) > 1e-6)
                            {
                                parametersChanged = true;
                                break;
                            }
                        }

                        if (parametersChanged)
                        {
                            correctedCount++;
                            Logger($"*** RE-EVALUATING CORRECTED PARTICLE {i + 1} ***");
                            Logger($"Corrected Parameters: B={Position[i, 0]:F3}, R={Position[i, 1]:F1}, D={Position[i, 2]:F0}, I={Position[i, 3]:F0}, A={Position[i, 4]:F0}");
                            if (turbaOutputModel.Check_PSI == "FALSE" || turbaOutputModel.Check_Thrust == "FALSE")
                            {
                                // **RE-RUN SIMULATION WITH CORRECTED PARAMETERS**
                                RunBlackboxApplication(Position[i, 0], Position[i, 1], Position[i, 2], Position[i, 3], Position[i, 4]);
                                thrustValues.Clear();
                                for (int lp = 0; lp < mxlp; lp++)
                                {
                                    thrustValues.Add(turbaOutputModel.OutputDataList[lp].Thrust);
                                }
                                // **RE-EVALUATE WITH CORRECTED VALUES**
                                currentEfficiency = turbaOutputModel.OutputDataList[0].Efficiency;
                                currentPower = turbaOutputModel.OutputDataList[0].Power_KW;
                                currentPenaltyScore = GetPenaltyScore();
                                CollectResultForCSV(
                                    turbineDataModel.InletPressure,
                                    turbineDataModel.InletTemperature,
                                    turbineDataModel.MassFlowRate,
                                    turbineDataModel.ExhaustPressure,
                                    Position[i, 0],
                                    Position[i, 1],
                                    Position[i, 2],
                                    Position[i, 3],
                                    Position[i, 4],
                                    currentEfficiency,
                                    currentPower,
                                    currentPenaltyScore,
                                    turbaOutputModel.OutputDataList[0].HOEHE,
                                    turbaOutputModel.OutputDataList[0].DELTA_T,
                                    turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure,
                                    turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature,
                                    turbaOutputModel.OutputDataList[0].Lang,
                                    turbaOutputModel.Check_PSI,
                                    turbaOutputModel.OutputDataList[0].GBC_Length,
                                    turbaOutputModel.OutputDataList[0].FMIN1,
                                    thrustValues
                                );

                                Logger($"After Correction - Efficiency: {currentEfficiency:F2}%, Penalty: {currentPenaltyScore:F2}");
                            }

                        }
                        else
                        {
                            Logger($"Particle {i + 1}: No parameter changes made during correction");
                        }

                        // If still infeasible after correction, reject the particle
                        if (currentPenaltyScore > 0)
                        {
                            Logger($"Particle {i + 1}: STILL INFEASIBLE after correction - Penalty: {currentPenaltyScore:F2}");
                            LogRelationshipCompliance(i);
                            continue; // Skip to next particle
                        }
                    }

                    // **PARTICLE IS NOW FEASIBLE - PROCESS IT**
                    feasibleCount++;
                    double currentFitness = currentEfficiency;
                    Logger($"*** FEASIBLE SOLUTION FOUND - Particle {i + 1} ***");
                    Logger($"Final Parameters: B={Position[i, 0]:F3}, R={Position[i, 1]:F1}, D={Position[i, 2]:F0}, I={Position[i, 3]:F0}, A={Position[i, 4]:F0}");
                    Logger($"Efficiency: {currentEfficiency:F6}%, Penalty: {currentPenaltyScore:F6}");

                    // Update personal best
                    if (currentFitness > BestFitness[i])
                    {
                        BestFitness[i] = currentFitness;
                        for (int j = 0; j < 5; j++)
                        {
                            BestPosition[i, j] = Position[i, j];
                        }

                        // Update global best
                        if (currentFitness > GlobalBestFitness)
                        {
                            GlobalBestFitness = currentFitness;
                            GlobalBestEfficiency = currentEfficiency;
                            for (int j = 0; j < 5; j++)
                            {
                                GlobalBestPosition[j] = Position[i, j];
                            }

                            Logger($"*** NEW GLOBAL BEST FOUND ***");
                            Logger($"Best Parameters: B={GlobalBestPosition[0]:F3}, R={GlobalBestPosition[1]:F1}, D={GlobalBestPosition[2]:F0}, I={GlobalBestPosition[3]:F0}, A={GlobalBestPosition[4]:F0}");
                            Logger($"Fitness: {GlobalBestFitness:F6}, Efficiency: {GlobalBestEfficiency:F6}%");
                            LogGlobalBestRelationships();
                        }
                    }

                    LogParticleResults(i, currentEfficiency, currentPenaltyScore, currentFitness);
                }
                catch (Exception ex)
                {
                    Logger($"Error evaluating particle {i + 1}: {ex.Message}");
                    Logger($"Failed Parameters: B={Position[i, 0]:F3}, R={Position[i, 1]:F1}, D={Position[i, 2]:F0}, I={Position[i, 3]:F0}, A={Position[i, 4]:F0}");
                }
            }

            Logger($"=== ITERATION SUMMARY ===");
            Logger($"Feasible particles: {feasibleCount}/{NumParticles}");
            Logger($"Blacklisted particles: {blacklistedCount}/{NumParticles}");
            Logger($"Corrected particles: {correctedCount}/{NumParticles}");

            // If no feasible solutions found, apply diversification
            if (feasibleCount == 0)
            {
                Logger("No feasible solutions found - applying emergency diversification");
                ApplyEmergencyDiversification();
            }
        }
        private void WriteAllResultsToCSV()
        {
            string filePath = @"C:\testDir\PSO_Results.csv";

            bool fileExists = File.Exists(filePath);

            using (var writer = new StreamWriter(filePath, append: true))
            {
                // If file doesn't exist, write header first
                if (!fileExists)
                {
                    string header = "InletPressure,InletTemperature,MassFlowRate,ExhaustPressure," +
                    "B,R,D,I,A,Efficiency,Power,Penalty," +
                    "HOEHE,DeltaT,WheelChamberPressure,WheelChamberTemp,Lang,CheckPSI,GBCLength,FMIN1";

                    for (int t = 1; t < mxlp; t++)
                    {
                        header += $",Thrust{t}";
                    }
                    writer.WriteLine(header);

                }

                foreach (var row in csvRows)
                {
                    writer.WriteLine(row);
                }
            }

            Logger($"CSV saved with {csvRows.Count} rows at {filePath}");
        }


        private bool IsBlacklistedCombination(int particleIndex)
        {
            double admission = Position[particleIndex, 0];
            double pressure = Position[particleIndex, 1];
            double stages = Position[particleIndex, 2];
            double shaft = Position[particleIndex, 3];
            double piston = Position[particleIndex, 4];

            

            if (admission <= 0.16)
            {
                Logger($"BLACKLIST: Admission too low ({admission:F3} <= 0.16)");
                return true;
            }

            if (admission <= 0.20 && pressure <= 16.0)
            {
                Logger($"BLACKLIST: Low admission + low pressure ({admission:F3}, {pressure:F1})");
                return true;
            }

            return false;

        }


        private void ApplyEmergencyDiversification()
        {
            Logger("Applying ENHANCED emergency diversification with proven ranges");

            for (int i = 0; i < NumParticles; i++)
            {
                // Use successful parameter ranges from the log
                Position[i, 0] = 0.14 + random.NextDouble() * 0.46; // Admission: 0.32-0.78 (successful range)
                Position[i, 1] = 15.0 + random.NextDouble() * 11.0; // Pressure: 17.0-28.0 (successful range)
                Position[i, 2] = -11 + random.NextDouble() * 5; // Stages: -11 to -6
                Position[i, 3] = 220 + random.NextDouble() * 10; // Shaft: 220-230 (successful range)
                Position[i, 4] = 250 + random.NextDouble() * 20; // Piston: 250-270 (successful range)

                // Apply bounds and discretization
                for (int j = 0; j < 5; j++)
                {
                    Position[i, j] = Math.Max(MinValues[j], Math.Min(MaxValues[j], Position[i, j]));
                    Position[i, j] = Math.Round(Position[i, j] / Steps[j]) * Steps[j];
                    Velocity[i, j] = 0;
                    BestPosition[i, j] = Position[i, j];
                }
                BestFitness[i] = double.MinValue;
            }
        }



        private void LogRelationshipCompliance(int particleIndex)
        {
            Logger($"Relationship Analysis for Particle {particleIndex + 1}:");
            Logger($"  Shaft Dia: {Position[particleIndex, 3]:F0} (thrust ↑)");
            Logger($"  Piston Dia: {Position[particleIndex, 4]:F0} (thrust ↓↓)");
            Logger($"  Admission: {Position[particleIndex, 0]:F3} (nozzle area ↑↑↑)");
            Logger($"  Pressure: {Position[particleIndex, 1]:F1} (wheel temp ↑↑↑)");
        }

        private void LogGlobalBestRelationships()
        {
            Logger("=== GLOBAL BEST RELATIONSHIP ANALYSIS ===");
            Logger($"Admission Factor: {GlobalBestPosition[0]:F6} (affects nozzle area ↑↑↑)");
            Logger($"Wheel Chamber Pressure: {GlobalBestPosition[1]:F3} (affects temp ↑↑↑, thrust ↓)");
            Logger($"Number of Stages: {GlobalBestPosition[2]:F0} (affects GBC length ↑↑↑, PSI ↓↓↓)");
            Logger($"Shaft Diameter: {GlobalBestPosition[3]:F0} (affects thrust ↑)");
            Logger($"Piston Diameter: {GlobalBestPosition[4]:F0} (affects thrust ↓↓)");

            // Check thrust optimization
            double thrustBalance = (MaxValues[4] - GlobalBestPosition[4]) / (GlobalBestPosition[3] - MinValues[3] + 1);
            Logger($"Thrust Balance Ratio: {thrustBalance:F2} (higher is better for thrust minimization)");
        }

        private void AnalyzeRelationshipPerformance()
        {
            Logger("=== RELATIONSHIP PERFORMANCE ANALYSIS ===");

            double avgShaft = 0, avgPiston = 0, avgAdmission = 0;
            for (int i = 0; i < NumParticles; i++)
            {
                avgShaft += Position[i, 3];
                avgPiston += Position[i, 4];
                avgAdmission += Position[i, 0];
            }
            avgShaft /= NumParticles;
            avgPiston /= NumParticles;
            avgAdmission /= NumParticles;

            Logger($"Average Shaft Diameter: {avgShaft:F2} (range: {MinValues[3]}-{MaxValues[3]})");
            Logger($"Average Piston Diameter: {avgPiston:F2} (range: {MinValues[4]}-{MaxValues[4]})");
            Logger($"Average Admission Factor: {avgAdmission:F3} (range: {MinValues[0]:F2}-{MaxValues[0]:F2})");

            // Check if thrust relationship is being followed
            bool thrustOptimized = (avgShaft < (MinValues[3] + MaxValues[3]) / 2) &&
                                  (avgPiston > (MinValues[4] + MaxValues[4]) / 2);
            Logger($"Thrust Relationship Compliance: {(thrustOptimized ? "GOOD" : "NEEDS IMPROVEMENT")}");
        }

        private void ApplyRelationshipGuidedDiversification()
        {
            Logger("Applying relationship-guided diversification");

            // Find particles with poor fitness and redirect them using relationships
            for (int i = 0; i < NumParticles; i++)
            {
                if (BestFitness[i] < 0) // Poor performing particles
                {
                    // Apply stronger relationship guidance
                    InitializeParticleWithRelationshipGuidance(i);
                    Logger($"Redirected particle {i + 1} using relationship guidance");
                }
            }
        }

        private void FinalEvaluation()
        {
            Logger("========== PERFORMING FINAL EVALUATION ==========");

            if (GlobalBestFitness > 0)
            {
                RunBlackboxApplication(GlobalBestPosition[0], GlobalBestPosition[1],
                                      GlobalBestPosition[2], GlobalBestPosition[3], GlobalBestPosition[4]);

                double finalPenalty = GetPenaltyScore();
                double finalEfficiency = turbaOutputModel.OutputDataList[0].Efficiency;

                Logger("========== FINAL OPTIMIZATION RESULTS ==========");
                Logger($"Final Efficiency: {finalEfficiency:F6}%");
                Logger($"Final Penalty Score: {finalPenalty:F6}");
                Logger($"All Constraints Satisfied: {(finalPenalty == 0 ? "YES" : "NO")}");
                Logger($"Optimal Parameters:");
                Logger($"  B (Admission Factor): {GlobalBestPosition[0]:F6}");
                Logger($"  R (Wheel Chamber Pressure): {GlobalBestPosition[1]:F3}");
                Logger($"  D (Number of Stages): {GlobalBestPosition[2]:F0}");
                Logger($"  I (Shaft Diameter): {GlobalBestPosition[3]:F0}");
                Logger($"  A (Balance Piston Diameter): {GlobalBestPosition[4]:F0}");

                LogGlobalBestRelationships();

                if (finalPenalty == 0)
                {
                    Logger("SUCCESS: All constraints satisfied! Relationship-aware optimization successful.");
                }
                double[] current = (double[])GlobalBestPosition.Clone();
                double bestPenalty = finalPenalty;
                double bestEfficiency = finalEfficiency;
                double[] bestParams = (double[])GlobalBestPosition.Clone();
                for (int var = 0; var < 5; var++)
                {
                    // Always start from the latest best parameters
                    current = (double[])bestParams.Clone();

                    double step = Steps[var];

                    if (var == 2) // Special handling for stages
                    {
                        int currentStages = (int)Math.Abs(bestParams[var]); // e.g. -9 => 9
                        int minStages = (int)Math.Abs(MinValues[var]);      // e.g. 11
                        int maxStages = (int)Math.Abs(MaxValues[var]);      // e.g. 6

                        // Loop through physical stage numbers (increasing means more stages physically)
                        for (int stages = currentStages + 1; stages <= minStages; stages++)
                        {
                            current = (double[])bestParams.Clone();
                            current[var] = -stages; // store negative for tool

                            RunBlackboxApplication(current[0], current[1], current[2], current[3], current[4]);
                            thrustValues.Clear();
                            for (int lp = 0; lp < mxlp; lp++)
                            {
                                thrustValues.Add(turbaOutputModel.OutputDataList[lp].Thrust);
                            }
                            double penalty = GetPenaltyScore();
                            double efficiency = turbaOutputModel.OutputDataList[0].Efficiency;
                            double power = turbaOutputModel.OutputDataList[0].Power_KW;
                            CollectResultForCSV(
                                turbineDataModel.InletPressure,
                                turbineDataModel.InletTemperature,
                                turbineDataModel.MassFlowRate,
                                turbineDataModel.ExhaustPressure,
                                current[0],
                                current[1],
                                current[2],
                                current[3],
                                current[4],
                                efficiency,
                                power,
                                penalty,
                                turbaOutputModel.OutputDataList[0].HOEHE,
                                turbaOutputModel.OutputDataList[0].DELTA_T,
                                turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure,
                                turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature,
                                turbaOutputModel.OutputDataList[0].Lang,
                                turbaOutputModel.Check_PSI,
                                turbaOutputModel.OutputDataList[0].GBC_Length,
                                turbaOutputModel.OutputDataList[0].FMIN1,
                                thrustValues
                            );
                            Logger(efficiency + " - Efficiency");

                            if (penalty != 0)
                                break;

                            if (efficiency > bestEfficiency)
                            {
                                bestEfficiency = efficiency;
                                bestPenalty = penalty;
                                bestParams = (double[])current.Clone();
                            }
                        }
                    }
                    else // Normal handling for other parameters
                    {
                        double minVal = bestParams[var];
                        double maxVal = MaxValues[var];

                        for (double i = minVal + step; i <= maxVal; i += step)
                        {
                            current = (double[])bestParams.Clone();
                            current[var] = i;

                            RunBlackboxApplication(current[0], current[1], current[2], current[3], current[4]);
                            thrustValues.Clear();
                            for (int lp = 0; lp < mxlp; lp++)
                            {
                                thrustValues.Add(turbaOutputModel.OutputDataList[lp].Thrust);
                            }
                            double penalty = GetPenaltyScore();
                            double efficiency = turbaOutputModel.OutputDataList[0].Efficiency;
                            double power = turbaOutputModel.OutputDataList[0].Power_KW;
                            Logger(efficiency + " - Efficiency");
                            CollectResultForCSV(
                                turbineDataModel.InletPressure,
                                turbineDataModel.InletTemperature,
                                turbineDataModel.MassFlowRate,
                                turbineDataModel.ExhaustPressure,
                                current[0],
                                current[1],
                                current[2],
                                current[3],
                                current[4],
                                efficiency,
                                power,
                                penalty,
                                turbaOutputModel.OutputDataList[0].HOEHE,
                                turbaOutputModel.OutputDataList[0].DELTA_T,
                                turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure,
                                turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature,
                                turbaOutputModel.OutputDataList[0].Lang,
                                turbaOutputModel.Check_PSI,
                                turbaOutputModel.OutputDataList[0].GBC_Length,
                                turbaOutputModel.OutputDataList[0].FMIN1,
                                thrustValues
                            );

                            // Additional nozzle height constraint for Admission Factor
                            if (var == 0)
                            {
                                double nozzleHeight = turbaOutputModel.OutputDataList[0].HOEHE;

                                if (penalty != 0)
                                    break;

                                if (!(nozzleHeight >= 10.5 && nozzleHeight <= 12.0))
                                    continue;
                            }

                            if (penalty != 0)
                                break;

                            if (efficiency > bestEfficiency)
                            {
                                bestEfficiency = efficiency;
                                bestPenalty = penalty;
                                bestParams = (double[])current.Clone();
                            }
                        }
                    }
                }


                // Report improvement, if any
                if (bestPenalty == 0 && bestEfficiency > GlobalBestFitness)
                {
                    Logger("=== 5-Pointer Manual Walk Improved the Solution! ===");
                    Logger($"Old Best Efficiency: {GlobalBestFitness:F6}%, New: {bestEfficiency:F6}%");

                    GlobalBestFitness = bestEfficiency;
                    GlobalBestEfficiency = bestEfficiency;
                    for (int i = 0; i < 5; i++) GlobalBestPosition[i] = bestParams[i];

                    RunBlackboxApplication(GlobalBestPosition[0], GlobalBestPosition[1],
                                      GlobalBestPosition[2], GlobalBestPosition[3], GlobalBestPosition[4]);

                     finalPenalty = GetPenaltyScore();
                     finalEfficiency = turbaOutputModel.OutputDataList[0].Efficiency;
                    Logger($"Improved Parameters after Manual Walk:");
                    Logger($"  B (Admission Factor): {GlobalBestPosition[0]:F6}");
                    Logger($"  R (Wheel Chamber Pressure): {GlobalBestPosition[1]:F3}");
                    Logger($"  D (Number of Stages): {GlobalBestPosition[2]:F0}");
                    Logger($"  I (Shaft Diameter): {GlobalBestPosition[3]:F0}");
                    Logger($"  A (Balance Piston Diameter): {GlobalBestPosition[4]:F0}");
                    Logger($"Final Efficiency: {finalEfficiency:F6}%");
                    Logger($"Final Penalty Score: {finalPenalty:F6}");
                    Logger($"All Constraints Satisfied: {(finalPenalty == 0 ? "YES" : "NO")}");
                    WriteAllResultsToCSV();

                }
                if (GlobalBestFitness > 0)
                {
                    Logger("SUCCESS: All constraints satisfied! Final optimization successful (after post-processing).");
                }


            }
            else
            {
                Logger("========== NO FEASIBLE SOLUTION FOUND ==========");
                Logger("Consider adjusting relationship strengths or parameter ranges");
            }
        }

        private void LogParticleResults(int particleIndex, double efficiency, double penaltyScore, double fitness)
        {
            Logger($"=============== Iteration/Particle: {IterationCounter}/{particleIndex + 1} Results ===============");
            Logger($"ACCEPTED: Eff={efficiency:F6}%, Penalty={penaltyScore:F6}, Fitness={fitness:F6}");
            Logger($"Personal Best: {BestFitness[particleIndex]:F6}");
            Logger($"Global Best: {GlobalBestFitness:F6}");
            Logger($"Position: B={Position[particleIndex, 0]:F6} R={Position[particleIndex, 1]:F3} D={Position[particleIndex, 2]:F1} I={Position[particleIndex, 3]:F0} A={Position[particleIndex, 4]:F0}");
            LogRelationshipCompliance(particleIndex);
            Logger("```````````````````````````````````````````````````````");
        }

        private string GetNozzleCheckType()
        {
            bool G14 = preFeasibilityDataModel.Decision == "TRUE";
            bool G26 = preFeasibilityDataModel.Decision_2 == "TRUE";
            if (G14) return "1120";
            if (!G14 && G26) return "1190";
            return "unknown";
        }

        private static bool TryParseDatFixedField(string line, int startIndex1Based, int length, out double value)
        {
            value = 0;
            if (string.IsNullOrEmpty(line) || startIndex1Based < 1 || startIndex1Based + length - 1 > line.Length)
                return false;
            var s = line.Substring(startIndex1Based - 1, length).Trim();
            return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Reads B,R,D,I,A from the nozzle DAT using the same layout as <see cref="UpdateDATSoftChecks"/>.</summary>
        private double[] TryReadDatParameters()
        {
            if (!File.Exists(nozzleDatFilePath))
            {
                Logger($"Read DAT: file not found at {nozzleDatFilePath}");
                return null;
            }

            try
            {
                var fileLines = File.ReadAllText(nozzleDatFilePath).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                string parameterB = "!ND   DM REGELR";
                string parameterR = "!     RADKAMMER";
                string parameterD = "!               DRUCKZIFFERN";
                string parameterI = "!               INNENDURCHMESSER";
                string parameterA = "!               AUSGLEICHSKOLBENDURCHMESSER";
                double b = 0, r = 0, d = 0, ii = 0, a = 0;
                bool fb = false, fr = false, fd = false, fi = false, fa = false;

                for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
                {
                    if (fileLines[lineNumber].Contains(parameterB) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fb = TryParseDatFixedField(fileLines[lineNumber], 17, 9, out b);
                    }
                    else if (fileLines[lineNumber].Contains(parameterR) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fr = TryParseDatFixedField(fileLines[lineNumber], 7, 9, out r);
                    }
                    else if (fileLines[lineNumber].Contains(parameterD) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fd = TryParseDatFixedField(fileLines[lineNumber], 17, 9, out d);
                    }
                    else if (fileLines[lineNumber].Contains(parameterI) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fi = TryParseDatFixedField(fileLines[lineNumber], 17, 9, out ii);
                    }
                    else if (fileLines[lineNumber].Contains(parameterA) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fa = TryParseDatFixedField(fileLines[lineNumber], 17, 9, out a);
                    }
                }

                if (!fb || !fr || !fd || !fi || !fa)
                {
                    Logger("Read DAT: could not parse all five parameters from markers.");
                    return null;
                }

                return new[] { b, r, d, ii, a };
            }
            catch (Exception ex)
            {
                Logger($"Read DAT error: {ex.Message}");
                return null;
            }
        }

        private double[] SnapVector(double[] vector)
        {
            var v = new double[5];
            for (int j = 0; j < 5; j++)
            {
                v[j] = Math.Max(MinValues[j], Math.Min(MaxValues[j], vector[j]));
                v[j] = Math.Round(v[j] / Steps[j]) * Steps[j];
            }
            return v;
        }

        private double[] ReadCurrentDatParametersAndSnap()
        {
            var raw = TryReadDatParameters();
            if (raw == null) return null;
            return SnapVector(raw);
        }

        /// <summary>
        /// Engineering limits on ERG outputs aligned with <see cref="HMBD.PSO_PenalityFunctionNozzle.PenaltyScoreCalculator"/> hard checks (BCD-specific).
        /// Gives the LLM context for how far outputs must move when adjusting B,R,D,I,A.
        /// </summary>
        private (object limits, string summary) BuildOutputLimitsForSnapshot(OutputLoadPoint o, string checkType)
        {
            double nozzleLo = configuration.GetValue<double?>("AppSettings:Nozzle_height_Limits_Lower") ?? 10.5;
            double nozzleHi = configuration.GetValue<double?>("AppSettings:Nozzle_height_Limits_Upper") ?? 27.0;
            int? nozzleAreaCfg = configuration.GetValue<int?>("AppSettings:Nozzle_Area_UpperLim");
            int fminUpper = (nozzleAreaCfg.HasValue && nozzleAreaCfg.Value > 0) ? nozzleAreaCfg.Value : 1430;
            const double thrustAbsLimit = 0.8;

            if (checkType == "1190")
            {
                double deltaTMax = 210;
                double inletT = preFeasibilityDataModel.TemperatureActualValue;
                double wheelP = o.Wheel_Chamber_Pressure;
                double wheelTempUpper = inletT <= 500
                    ? WheelChamberTempCurveWithCW1GetUpperLimit(wheelP)
                    : WheelChamberTempCurveWithCW2GetUpperLimit(wheelP);
                bool varicode7 = false;
                double gbcMax = varicode7 ? 356 : 370;

                var limits = new
                {
                    bcd = "1190",
                    HOEHE = new { lowerLimit = nozzleLo, upperLimit = nozzleHi, unit = "nozzle height (penalty band)" },
                    FMIN1 = new { lowerLimitExclusive = 0, upperLimit = fminUpper, note = "nozzle area G1; must be > 0 and <= upperLimit" },
                    DELTA_T = new { lowerLimitExclusive = 0, upperLimit = deltaTMax, note = "GBC delta T" },
                    Wheel_Chamber_Temperature = new { lowerLimitExclusive = 0, upperLimit = wheelTempUpper, note = "upper from CW curve at current wheel chamber P; inlet-based CW1 vs CW2" },
                    Wheel_Chamber_Pressure = new { note = "used with inlet T to set wheel temp ceiling; not a simple fixed bound" },
                    GBC_Length = new { lowerLimitExclusive = 0, upperLimit = gbcMax, note = "varicode7=false => 370 else 356" },
                    thrustPerLp = new { perLoadPoint_absMax = thrustAbsLimit, note = "each load point thrust must stay in [-0.8, 0.8]" },
                    Check_PSI = new { mustBe = "TRUE", note = "PSI gate from Turba; failing rows drive penalty" },
                    Lang = new { mustBeString = "TRUE", note = "LANG check" },
                    Efficiency = new { note = "informational for optimization objective; not a hard band in penalty JSON" },
                    Power_KW = new { note = "informational" }
                };

                string summary =
                    $"BCD1190 output bands: HOEHE [{nozzleLo},{nozzleHi}]; FMIN1 in (0,{fminUpper}]; DELTA_T in (0,{deltaTMax}]; " +
                    $"Wheel_T <= {wheelTempUpper:F1} (curve at P={wheelP:F3}); GBC_Length in (0,{gbcMax}]; " +
                    $"each thrust in [-{thrustAbsLimit},{thrustAbsLimit}]; PSI/Lang checks must pass.";
                return (limits, summary);
            }

            if (checkType == "1120")
            {
                double deltaTMax = 240;
                double wheelTempMax = 410;
                double gbcMax = 318;

                var limits = new
                {
                    bcd = "1120",
                    HOEHE = new { lowerLimit = nozzleLo, upperLimit = nozzleHi, unit = "nozzle height (penalty band)" },
                    FMIN1 = new { lowerLimitExclusive = 0, upperLimit = fminUpper, note = "nozzle area G1" },
                    DELTA_T = new { lowerLimitExclusive = 0, upperLimit = deltaTMax, note = "GBC delta T" },
                    Wheel_Chamber_Temperature = new { lowerLimitExclusive = 0, upperLimit = wheelTempMax, note = "fixed ceiling °C for BCD1120" },
                    Wheel_Chamber_Pressure = new { note = "informational for thermodynamics" },
                    GBC_Length = new { lowerLimitExclusive = 0, upperLimit = gbcMax, note = "GBC length cap" },
                    thrustPerLp = new { perLoadPoint_absMax = thrustAbsLimit, note = "each in [-0.8, 0.8]" },
                    Check_PSI = new { mustBe = "TRUE", note = "PSI gate" },
                    Lang = new { mustBeString = "TRUE", note = "LANG check (1120 reads output Lang)" },
                    Efficiency = new { note = "informational" },
                    Power_KW = new { note = "informational" }
                };

                string summary =
                    $"BCD1120 output bands: HOEHE [{nozzleLo},{nozzleHi}]; FMIN1 in (0,{fminUpper}]; DELTA_T in (0,{deltaTMax}]; " +
                    $"Wheel_T in (0,{wheelTempMax}]; GBC_Length in (0,{gbcMax}]; thrust per LP in [-{thrustAbsLimit},{thrustAbsLimit}]; PSI/Lang must pass.";
                return (limits, summary);
            }

            var fallback = new
            {
                bcd = checkType,
                HOEHE = new { lowerLimit = nozzleLo, upperLimit = nozzleHi },
                FMIN1 = new { upperLimit = fminUpper },
                note = "Unknown BCD; use parameterLimits for DAT knobs; verify penalty source for exact output bands."
            };
            return (fallback, "BCD unknown: partial limits only; confirm checkType.");
        }

        private string BuildOllamaSnapshotJson(double[] vector, double penalty, double efficiency)
        {
            var o = turbaOutputModel.OutputDataList[0];
            var checkType = GetNozzleCheckType();
            var (outputLimits, outputLimitsSummary) = BuildOutputLimitsForSnapshot(o, checkType);

            var checks = new Dictionary<string, string>
            {
                ["Check_HOEHE"] = turbaOutputModel.Check_HOEHE,
                ["Check_FMIN1"] = turbaOutputModel.Check_FMIN1,
                ["Check_DELTA_T"] = turbaOutputModel.Check_DELTA_T,
                ["Check_Wheel_Chamber_Temperature"] = turbaOutputModel.Check_Wheel_Chamber_Temperature,
                ["Check_GBC_Length"] = turbaOutputModel.Check_GBC_Length,
                ["Check_PSI"] = turbaOutputModel.Check_PSI,
                ["Check_Lang"] = turbaOutputModel.Check_Lang,
                ["Check_Thrust"] = turbaOutputModel.Check_Thrust,
            };
            int nlp = mxlp > 0 ? mxlp : Math.Min(10, turbaOutputModel.OutputDataList.Count);
            var thrustSlice = turbaOutputModel.OutputDataList.Take(nlp).Select(lp => lp.Thrust).ToList();

            var parameterLimits = new
            {
                B = new { description = "BEAUFSCHL (admission)", lowerLimit = MinValues[0], upperLimit = MaxValues[0], step = Steps[0] },
                R = new { description = "RADKAMMER (wheel chamber pressure)", lowerLimit = MinValues[1], upperLimit = MaxValues[1], step = Steps[1] },
                D = new { description = "DRUCKZIFFERN (stages; negative encoding as in DAT)", lowerLimit = MinValues[2], upperLimit = MaxValues[2], step = Steps[2] },
                I = new { description = "INNENDURCHMESSER (shaft diameter)", lowerLimit = MinValues[3], upperLimit = MaxValues[3], step = Steps[3] },
                A = new { description = "AUSGLEICHSKOLBEN (balance piston)", lowerLimit = MinValues[4], upperLimit = MaxValues[4], step = Steps[4] }
            };

            string limitsSummary =
                $"B: lowerLimit={MinValues[0]:G9} upperLimit={MaxValues[0]:G9} step={Steps[0]:G9}; " +
                $"R: lowerLimit={MinValues[1]:G9} upperLimit={MaxValues[1]:G9} step={Steps[1]:G9}; " +
                $"D: lowerLimit={MinValues[2]:G9} upperLimit={MaxValues[2]:G9} step={Steps[2]:G9}; " +
                $"I: lowerLimit={MinValues[3]:G9} upperLimit={MaxValues[3]:G9} step={Steps[3]:G9}; " +
                $"A: lowerLimit={MinValues[4]:G9} upperLimit={MaxValues[4]:G9} step={Steps[4]:G9}. " +
                "Every proposed value must satisfy lowerLimit <= value <= upperLimit and align to step (snap to grid).";

            var snapshot = new
            {
                checkType,
                current = new { B = vector[0], R = vector[1], D = vector[2], I = vector[3], A = vector[4] },
                parameterLimits,
                limitsSummary,
                outputLimits,
                outputLimitsSummary,
                bounds = new[]
                {
                    new { name = "B", lowerLimit = MinValues[0], upperLimit = MaxValues[0], step = Steps[0], min = MinValues[0], max = MaxValues[0] },
                    new { name = "R", lowerLimit = MinValues[1], upperLimit = MaxValues[1], step = Steps[1], min = MinValues[1], max = MaxValues[1] },
                    new { name = "D", lowerLimit = MinValues[2], upperLimit = MaxValues[2], step = Steps[2], min = MinValues[2], max = MaxValues[2] },
                    new { name = "I", lowerLimit = MinValues[3], upperLimit = MaxValues[3], step = Steps[3], min = MinValues[3], max = MaxValues[3] },
                    new { name = "A", lowerLimit = MinValues[4], upperLimit = MaxValues[4], step = Steps[4], min = MinValues[4], max = MaxValues[4] },
                },
                penalty,
                efficiency,
                checks,
                outputs = new
                {
                    o.HOEHE,
                    o.FMIN1,
                    o.DELTA_T,
                    o.Wheel_Chamber_Pressure,
                    o.Wheel_Chamber_Temperature,
                    o.GBC_Length,
                    o.Efficiency,
                    o.Power_KW,
                    o.Lang,
                    thrustPerLp = thrustSlice
                },
                note = "Propose only B,R,D,I,A. Respect parameterLimits/limitsSummary for inputs and outputLimits/outputLimitsSummary for Turba ERG feasibility (penalty checks); invalid DAT values are rejected by the optimizer."
            };
            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        }

        private static string TruncateForLog(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= maxLen) return s;
            return s.Substring(0, maxLen) + "...";
        }

        /// <summary>Ollama proposes next DAT parameters; requires local Ollama and model (see appsettings Ollama section).</summary>
        public void InvokeOllamaGuidedOptimizer()
        {
            InitializeParameterBounds();
            var vector = ReadCurrentDatParametersAndSnap();
            if (vector == null)
            {
                Logger("Ollama-guided mode: failed to read seed B,R,D,I,A from DAT.");
                return;
            }

            int maxIter = configuration.GetValue<int>("Ollama:MaxIterations");
            if (maxIter <= 0) maxIter = 25;

            GlobalBestFitness = double.MinValue;
            GlobalBestEfficiency = 0;

            using (var advisor = new OllamaTurbaAdvisor(configuration, Logger))
            {
                for (int iter = 1; iter <= maxIter; iter++)
                {
                    Logger($"=== Ollama-guided iteration {iter}/{maxIter} ===");
                    Logger($"Parameters: B={vector[0]:F3}, R={vector[1]:F1}, D={vector[2]:F0}, I={vector[3]:F0}, A={vector[4]:F0}");

                    RunBlackboxApplication(vector[0], vector[1], vector[2], vector[3], vector[4]);

                    thrustValues.Clear();
                    int nlp = mxlp > 0 ? mxlp : turbaOutputModel.OutputDataList.Count;
                    for (int lp = 0; lp < nlp && lp < turbaOutputModel.OutputDataList.Count; lp++)
                        thrustValues.Add(turbaOutputModel.OutputDataList[lp].Thrust);

                    double penalty = GetPenaltyScore();
                    double efficiency = turbaOutputModel.OutputDataList[0].Efficiency;
                    double power = turbaOutputModel.OutputDataList[0].Power_KW;
                    CollectResultForCSV(
                        turbineDataModel.InletPressure,
                        turbineDataModel.InletTemperature,
                        turbineDataModel.MassFlowRate,
                        turbineDataModel.ExhaustPressure,
                        vector[0], vector[1], vector[2], vector[3], vector[4],
                        efficiency,
                        power,
                        penalty,
                        turbaOutputModel.OutputDataList[0].HOEHE,
                        turbaOutputModel.OutputDataList[0].DELTA_T,
                        turbaOutputModel.OutputDataList[0].Wheel_Chamber_Pressure,
                        turbaOutputModel.OutputDataList[0].Wheel_Chamber_Temperature,
                        turbaOutputModel.OutputDataList[0].Lang,
                        turbaOutputModel.Check_PSI,
                        turbaOutputModel.OutputDataList[0].GBC_Length,
                        turbaOutputModel.OutputDataList[0].FMIN1,
                        thrustValues
                    );

                    if (penalty == 0)
                    {
                        Logger($"Ollama-guided mode: feasible (penalty=0) at iteration {iter}. Efficiency={efficiency:F6}%");
                        GlobalBestFitness = efficiency;
                        GlobalBestEfficiency = efficiency;
                        for (int j = 0; j < 5; j++) GlobalBestPosition[j] = vector[j];
                        break;
                    }

                    string snapshot = BuildOllamaSnapshotJson(vector, penalty, efficiency);
                    var proposed = advisor.ProposeNextParameters(snapshot, out string rawOut);
                    if (!string.IsNullOrEmpty(rawOut))
                        Logger($"Ollama assistant output ({TruncateForLog(rawOut, 800)})");

                    if (proposed == null)
                    {
                        Logger("Ollama returned no valid proposal; using ApplyConstraintSpecificCorrection as fallback.");
                        for (int j = 0; j < 5; j++) Position[0, j] = vector[j];
                        ApplyConstraintSpecificCorrection(0, penalty);
                        vector = new double[5];
                        for (int j = 0; j < 5; j++) vector[j] = Position[0, j];
                        vector = SnapVector(vector);
                    }
                    else
                    {
                        vector = SnapVector(proposed);
                    }
                }
            }

            Logger($"Ollama-guided mode finished. GlobalBestEfficiency={GlobalBestEfficiency:F6} GlobalBestFitness={GlobalBestFitness:F6}");
        }

        // Rest of the methods remain the same as in your original code...
        public void RunBlackboxApplication(double variableB, double variableR, double variableD, double variableI, double variableA)
        {
            UpdateDATSoftChecks(variableB, variableR, variableD, variableI, variableA);
            LaunchTurba();
        }

        public void UpdateDATSoftChecks(double variableB, double variableR, double variableD, double variableI, double variableA)
        {
            string filePath = nozzleDatFilePath;

            if (!File.Exists(filePath))
            {
                Logger($"Error: File not found at {filePath}");
                return;
            }

            try
            {
                string fileContent = File.ReadAllText(filePath);
                var fileLines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                string parameterB = "!ND   DM REGELR";
                string parameterR = "!     RADKAMMER";
                string parameterD = "!               DRUCKZIFFERN";
                string parameterI = "!               INNENDURCHMESSER";
                string parameterA = "!               AUSGLEICHSKOLBENDURCHMESSER";

                for (int lineNumber = 0; lineNumber < fileLines.Length; lineNumber++)
                {
                    if (fileLines[lineNumber].Contains(parameterB) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 17, 9, variableB);
                        fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 60, 9, 0, "000000000");
                    }
                    else if (fileLines[lineNumber].Contains(parameterR) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 7, 9, variableR);
                    }
                    else if (fileLines[lineNumber].Contains(parameterD) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 17, 9, variableD, "0000.000");
                    }
                    else if (fileLines[lineNumber].Contains(parameterI) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 17, 9, variableI);
                    }
                    else if (fileLines[lineNumber].Contains(parameterA) && lineNumber + 1 < fileLines.Length)
                    {
                        lineNumber++;
                        fileLines[lineNumber] = UpdateLine(fileLines[lineNumber], 17, 9, variableA);
                    }
                }

                fileContent = string.Join(Environment.NewLine, fileLines);

                int retryCount = 0;
                while (!IsFileReadyForOpen(filePath) && retryCount < 20)
                {
                    System.Threading.Thread.Sleep(50);
                    retryCount++;
                }

                File.WriteAllText(filePath, fileContent);
                Logger($"DAT file updated with constraint-corrected parameters");
            }
            catch (Exception ex)
            {
                Logger($"Error updating DAT file: {ex.Message}");
            }
        }

        private string UpdateLine(string line, int startIndex, int length, double value, string format = "00000.000")
        {
            if (string.IsNullOrEmpty(line) || startIndex < 1 || startIndex + length - 1 > line.Length)
            {
                Logger($"Warning: Invalid line update parameters");
                return line ?? "";
            }

            string formattedValue = value.ToString(format);
            if (formattedValue.Length > length)
            {
                formattedValue = formattedValue.Substring(0, length);
            }
            else if (formattedValue.Length < length)
            {
                formattedValue = formattedValue.PadLeft(length);
            }

            return line.Substring(0, startIndex - 1) + formattedValue + line.Substring(startIndex + length - 1);
        }

        private bool IsFileReadyForOpen(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        private void Logger(string message)
        {
            logger.LogInformation(message);
        }

        private double GetPenaltyScore()
        {
            PenaltyScoreCalculator penaltyScoreCalculator = new PenaltyScoreCalculator();
            return penaltyScoreCalculator.GetPenaltyScore();
        }

        private void LaunchTurba()
        {
            CuTurbaAutomation cuTurbaAutomation = new CuTurbaAutomation();
            cuTurbaAutomation.LaunchTurba();
        }
    }
}



