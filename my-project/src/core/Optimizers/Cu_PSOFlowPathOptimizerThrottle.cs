// namespace Optimizers.Cu_PSOFlowPathOptimizerThrottle;
// using System;
// using System.IO;
// using System.Linq;
// using Microsoft.Extensions.Configuration;
// using Models.LoadPointDataModel;
// using Models.PreFeasibility;
// using Models.TurbaOutputDataModel;
// using Models.TurbineData;
// // soft checks
// public class PSOFlowPathOptimizerThrottle
// {
//     private IConfiguration configuration;
//     private LoadPointDataModel loadPointDataModel;
//     private TurbineDataModel turbineDataModel;
//     private PreFeasibilityDataModel preFeasibilityDataModel;
//     private TurbaOutputModel turbaOutputModel;

//     private int NoofIteration;
//     public PSOFlowPathOptimizerThrottle(){
//         configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("src/core/Config/appsettings.json", optional: false, reloadOnChange: true).Build();
//         NoofIteration = configuration.GetValue<int>("AppSettings:No_of_Iteration");
//         loadPointDataModel = LoadPointDataModel.getInstance();
//         turbineDataModel = TurbineDataModel.getInstance();
//         preFeasibilityDataModel = PreFeasibilityDataModel.getInstance();
//         turbaOutputModel = TurbaOutputModel.getInstance();
//     }
//     const int NumParticles_th = 1; // Number of particles in the swarm
//     int ParticleCount_th;
//     double[,] Position_th = new double[NumParticles_th, 3]; // Positions of particles
//     double[,] Velocity_th = new double[NumParticles_th, 3]; // Velocities of particles
//     double[,] BestPosition_th = new double[NumParticles_th, 3]; // Best known positions of particles
//     double[] BestEfficiency_th = new double[NumParticles_th]; // Best known efficiencies of particles
//     double[] GlobalBestPosition_th = new double[3]; // Best known position in the entire swarm
//     double GlobalBestEfficiency_th; // Best known efficiency in the entire swarm

//     double[] BestFitness_th = new double[NumParticles_th]; // Best known fitness of particles
//     double GlobalBestFitness_th; // Best known fitness in the entire swarm

//     // Variable ranges
//     double[] MinValues_th = new double[3]; // Minimum values for each variable
//     double[] MaxValues_th = new double[3]; // Maximum values for each variable
//     double[] Steps_th = new double[3]; // Step sizes for each variable

//     double BaseLPMassFlow_th;
//     int IterationCounter_th;

//     public void InvokeTurbineDesigner_th()
//     {
//         // var LoadPoints = GetWorksheet("LOAD_POINTS");
//         BaseLPMassFlow_th =  loadPointDataModel.LoadPoints[0].MassFlow;//Convert.ToDouble(LoadPoints.Range("B4").Value);
//         PSO_loop_th();
//         Logger("___________________________________________________");
//     }

//     public void PSO_loop_th()
//     {
//         int Iterations;
//         int Iter;
//         // var Admin = GetWorksheet("ADMIN_CONTROLS");

//         Iterations = NoofIteration;//Convert.ToInt32(Admin.Range("B2").Value); // Number of iterations for the optimization process
//         InitializeParticles_th();

//         for (Iter = 1; Iter <= Iterations; Iter++)
//         {
//             Logger($"################ ITERATION: {Iter} ##################");
//             IterationCounter_th = Iter;
//             EvaluateAndUpdateParticles_th();
//             UpdateParticles_th();

//             if (GlobalBestFitness_th > 90)
//             {
//                 break;
//             }
//         }

//         if (GlobalBestFitness_th < 80)
//         {
//             Logger("TURBINE DESIGN FAILED: ");
//         }
//         else
//         {
//             Logger("TURBINE DESIGN SUCCESSFUL !! GOOD TO GO...");
//             RunBlackboxApplication_th(GlobalBestPosition_th[0], GlobalBestPosition_th[1], GlobalBestPosition_th[2]);
//             Logger($"Final Penalty Score: {GetPenaltyScore_th()}");
//         }

//         Logger($"Global Best Fitness: {GlobalBestFitness_th}");
//         Logger($"Global Best Position: B={GlobalBestPosition_th[0]} R={GlobalBestPosition_th[1]} D={GlobalBestPosition_th[2]}");
//     }

//     public void InitializeParticles_th()
//     {
//         string [] fileLines = turbineDataModel.DAT_DATA;
//         // var Prefeas = GetWorksheet("Pre-Feasibility checks");
//         // var Dat_Data = GetWorksheet("DAT_DATA");
//         double inletpressure = preFeasibilityDataModel.InletPressureActualValue; //Convert.ToDouble(Prefeas.Range("F7").Value);

//         double const_BEAUFSCHL = turbineDataModel.BEAUFSCHL;//Convert.ToDouble(Dat_Data.Range("E2").Value);
//         double const_RADKAMMER = turbineDataModel.RADKAMMER;//Convert.ToDouble(Dat_Data.Range("E3").Value);
//         double const_DRUCKZIFF = turbineDataModel.DRUCK;//Convert.ToDouble(Dat_Data.Range("E4").Value);

//         MinValues_th[0] = -13; MaxValues_th[0] = -6; Steps_th[0] = 1;
//         MinValues_th[1] = 200; MaxValues_th[1] = 230; Steps_th[1] = 5;
//         MinValues_th[2] = 240; MaxValues_th[2] = 270; Steps_th[2] = 5;

//         Position_th[0, 0] = -11;
//         Position_th[0, 1] = 230;
//         Position_th[0, 2] = 260;

//         for (int i = 1; i < NumParticles_th; i++)
//         {
//             for (int j = 0; j < 3; j++)
//             {
//                 Position_th[i, j] = MinValues_th[j] + new Random().NextDouble() * (MaxValues_th[j] - MinValues_th[j]);
//                 Position_th[i, j] = Math.Round(Position_th[i, j] / Steps_th[j]) * Steps_th[j];
//                 Velocity_th[i, j] = (new Random().NextDouble() - 0.5) * Steps_th[j];
//                 BestPosition_th[i, j] = Position_th[i, j];
//             }
//             BestFitness_th[i] = -1;
//         }

//         GlobalBestFitness_th = -1;
//     }

//     public void UpdateParticles_th()
//     {
//         double w = 0.5; // Inertia weight
//         double c1 = 1.5; // Cognitive (particle) coefficient
//         double c2 = 1.5; // Social (swarm) coefficient

//         for (int i = 0; i < NumParticles_th; i++)
//         {
//             for (int j = 0; j < 3; j++)
//             {
//                 double r1 = new Random().NextDouble();
//                 double r2 = new Random().NextDouble();

//                 Velocity_th[i, j] = w * Velocity_th[i, j] +
//                                     c1 * r1 * (BestPosition_th[i, j] - Position_th[i, j]) +
//                                     c2 * r2 * (GlobalBestPosition_th[j] - Position_th[i, j]);

//                 Velocity_th[i, j] += (new Random().NextDouble() - 0.5) * 0.1 * (MaxValues_th[j] - MinValues_th[j]);

//                 Position_th[i, j] += Velocity_th[i, j];

//                 if (Position_th[i, j] < MinValues_th[j])
//                 {
//                     Position_th[i, j] = MinValues_th[j];
//                 }
//                 else if (Position_th[i, j] > MaxValues_th[j])
//                 {
//                     Position_th[i, j] = MaxValues_th[j];
//                 }

//                 Position_th[i, j] = Math.Round(Position_th[i, j] / Steps_th[j]) * Steps_th[j];
//             }
//         }
//     }

//     public void EvaluateAndUpdateParticles_th()
//     {
//         // var output = GetWorksheet("Output");

//         for (int i = 0; i < NumParticles_th; i++)
//         {
//             RunBlackboxApplication_th(Position_th[i, 0], Position_th[i, 1], Position_th[i, 2]);
//             double currentEfficiency = turbaOutputModel.OutputDataList[0].Efficiency;//Convert.ToDouble(output.Range("R2").Value);
//             double currentPenaltyScore = GetPenaltyScore_th();
//             double currentFitness = currentEfficiency - currentPenaltyScore;

//             bool currentBoolean = currentPenaltyScore <= 0;

//             if (currentFitness > BestFitness_th[i])
//             {
//                 BestFitness_th[i] = currentFitness;
//                 BestEfficiency_th[i] = currentEfficiency;

//                 for (int j = 0; j < 3; j++)
//                 {
//                     BestPosition_th[i, j] = Position_th[i, j];
//                 }

//                 if (currentFitness > GlobalBestFitness_th)
//                 {
//                     GlobalBestFitness_th = currentFitness;
//                     GlobalBestEfficiency_th = currentEfficiency;
//                     for (int j = 0; j < 3; j++)
//                     {
//                         GlobalBestPosition_th[j] = Position_th[i, j];
//                     }
//                 }
//             }

//             Logger($"===============  Iteration/Particle: {IterationCounter_th}/{i} Results   =============== ");
//             Logger($"Global best Fitness | Eff : {GlobalBestFitness_th}|{GlobalBestEfficiency_th}");
//             Logger($"This Iteration best fitness: {BestFitness_th[i]}");

//             Logger("AddFactor | PressWHC | Stages | ShaftDia | PistonDia");
//             Logger($"{Position_th[i, 0]}|{Position_th[i, 1]}|{Position_th[i, 2]}");

//             Logger("Penalty | Fitness| Efficiency ");
//             Logger($"{currentPenaltyScore}|{currentFitness}|{currentEfficiency}");
//             Logger("````````````````````````````````````````````````````````` ");
//         }
//     }

//     public void RunBlackboxApplication_th(double VariableD, double VariableI, double VariableA)
//     {
//         UpdateDAT_SoftChecks_th(VariableD, VariableI, VariableA);
//         LaunchTurba();
//     }

//     public void UpdateDAT_SoftChecks_th(double VariableD, double VariableI, double VariableA)
//     {
//         string filePath = @"C:\testDir\TURBATURBAE1.DAT.DAT";
//         string fileContent;

//         RetryFile:
//         if (Is
//         ForOpen(filePath))
//         {
//             fileContent = File.ReadAllText(filePath);
//         }
//         else
//         {
//             goto RetryFile;
//         }

//         var fileLines = fileContent.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList();

//         string ParameterD = "!               DRUCKZIFFERN";
//         string ParameterI = "!               INNENDURCHMESSER";
//         string ParameterA = "!               AUSGLEICHSKOLBENDURCHMESSER";

//         for (int lineNumber = 0; lineNumber < fileLines.Count; lineNumber++)
//         {
//             if (fileLines[lineNumber].Contains(ParameterD))
//             {
//                 lineNumber++;
//                 fileLines[lineNumber] = fileLines[lineNumber].Substring(0, 16)+ VariableD.ToString("0000.000") + fileLines[lineNumber].Substring(25);
//             }
//             if (fileLines[lineNumber].Contains(ParameterI))
//             {
//                 lineNumber++;
//                 fileLines[lineNumber] = fileLines[lineNumber].Substring(0, 16) + VariableI.ToString("00000.000") + fileLines[lineNumber].Substring(25);
//             }
//             if (fileLines[lineNumber].Contains(ParameterA))
//             {
//                 lineNumber++;
//                 fileLines[lineNumber] = fileLines[lineNumber].Substring(0, 16) + VariableA.ToString("00000.000") + fileLines[lineNumber].Substring(25);
//             }
//         }

//         fileContent = string.Join("\r\n", fileLines);

//         RetryFile2:
//         if (IsFileReadyForOpen(filePath))
//         {
//             File.WriteAllText(filePath, fileContent);
//         }
//         else
//         {
//             goto RetryFile2;
//         }

//         Logger("Dat file updated with following parameters-");
//         Logger("Stages | ShaftDia | PistonDia");
//         Logger($"{VariableD} | {VariableI} | {VariableA}");
//     }

//     private void Logger(string message)
//     {
//         Console.WriteLine(message);
//     }

    

//     private bool IsFileReadyForOpen(string filePath)
//     {
//         try
//         {
//             using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
//             {
//                 stream.Close();
//             }
//         }
//         catch (IOException)
//         {
//             return false;
//         }

//         return true;
//     }

//     private double GetPenaltyScore_th()
//     {
//         // Implement this method to return the penalty score
//         // This is a placeholder implementation
//         return 0;
//     }

//     private void LaunchTurba()
//     {
//         // Implement this method to launch the Turba application
//         // This is a placeholder implementation
//     }
// }
