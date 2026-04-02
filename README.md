# **Queuing Model**
This system is a comprehensive C# simulation tool designed to model and analyze various queueing scenarios (M/M/1, M/G/1, G/G/1, and M/M/c) by comparing real-time simulated data with theoretical mathematical formulas.
Below is a detailed explanation of each file and its role within the system.
#### 1. **Root Directory**
| File                        | Purpose                   | Working Logic                                                                                                                                                                                           |
| :-------------------------- | :------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **`Program.cs`**            | **Main Entry Point**      | Manages the user interface (CLI). It handles the main menu, prompts for input parameters (arrival rate λ, service rate μ, etc.), and coordinates between the simulation engine and the reporting tools. |
| **`QueueingSystem.csproj`** | **Project Configuration** | A standard .NET project file that defines dependencies, target framework, and build settings.  |

#### 2. **Models Folder (/Models)**
This folder contains the data structures and mathematical foundations of the system.
| File                      | Purpose                     | Working Logic                                                                                                                                                                                                  |
| :------------------------ | :-------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`Customer.cs`**         | **Entity Representation**   | Defines the `Customer` class. It tracks individual timestamps: when they arrived, when service started, and when it ended. It calculates derived metrics like `WaitInQueue` and `TimeInSystem`.                |
| **`Server.cs`**           | **Resource Representation** | Defines the `Server` class. It tracks whether a server is busy or idle and calculates its own utilization rate based on the total time spent serving customers.                                                |
| **`QueueFormulas.cs`**    | **Mathematical Engine**     | Contains static methods for calculating theoretical queueing metrics (Little's Law, Erlang C, Kingman’s approximation). It provides the "perfect" mathematical baseline to compare against simulation results. |
| **`SimulationResult.cs`** | **Data Container**          | A structured class used to store the final output of a simulation run, including both simulated averages and theoretical expectations for easy comparison.                                                     |

#### 3. **Simulation Folder (/Simulation)**
This folder contains the logic for running the discrete-event simulations.
| File                    | Purpose                   | Working Logic                                                                                                                                                                         |
| :---------------------- | :------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **`EventEngine.cs`**    | **Core Simulator**        | The "brain" of the simulation. It uses a time-stepped approach to process arrivals and departures. It manages the queue logic (FIFO) and assigns customers to available servers.      |
| **`MM1Simulator.cs`**   | **Specialized Simulator** | A simplified version of the engine specifically optimized for the M/M/1 model (Single server, exponential arrivals/service).                                                          |
| **`RandomVariates.cs`** | **Stochastic Generator**  | Generates random numbers following specific distributions (Exponential, Erlang, Normal, Log-Normal). This allows the system to simulate "real-world" randomness in customer behavior. |

#### 4. **Reports Folder (/Reports)**
This folder handles the output and visualization of the simulation data.
| File                    | Purpose               | Working Logic                                                                                                                                      |
| :---------------------- | :-------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`ConsoleDisplay.cs`** | **CLI Visualization** | Formats and prints simulation results and comparison tables directly to the terminal using colors and structured layouts.                          |
| **`ReportExporter.cs`** | **Data Persistence**  | Handles file I/O. It can export simulation results into **.txt** summaries for reading or **.csv** files for further analysis in tools like Excel. |

####**Here's the complete Simulation File of the Project:**
https://replit.com/join/ddrovltoxq-aishakhanpk27

#### Summary of Workflow
1. Input: User selects a model and provides rates (λ and μ) in Program.cs.
2. Generation: RandomVariates.cs creates a sequence of random arrival and service times.
3. Execution: EventEngine.cs processes these events, moving Customer objects through Server objects.
4. Calculation: QueueFormulas.cs calculates what the results should be mathematically.
5. Output: ConsoleDisplay.cs shows the results, and ReportExporter.cs saves them to disk.
