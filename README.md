# MetaLogger

MetaLogger is a lightweight C# logging library that supports both static and instance-based logging. It includes features such as debug mode, custom log directories, file suffixes, and caller information. The instance-based logger also allows you to add custom prefixes or suffixes to every log message.

## Features

- **Static Logger (MetaLogger)**
  - Quick access via static methods
  - Supports logging information, warnings, errors, debug messages, and crash logs
  - Optionally includes caller information in log messages
  - Customizable log directory and file name suffix

- **Instance Logger (InstanceLogger)**
  - Allows multiple logger instances with different configurations
  - Supports custom message prefixes and suffixes
  - Same logging levels as the static logger (information, warning, error, debug, and crash)
  - Customizable log directory and file name suffix

## Installation

### Install via NuGet

You can install MetaLogger from NuGet:

```bash
Install-Package MetaLogger
```

Or via the .NET CLI:

```bash
dotnet add package MetaLogger
```

### Clone the Repository (Alternative Installation)

```bash
git clone https://github.com/Jacobwasbeast/MetaLogger.git
```

### Include in Your Project

Add the MetaLogger project or its source files to your solution. Then add a reference from your main project to the MetaLogger project.

## Basic Setup

### Static Logger

The static logger is available via the `MetaLogger` class. It is ideal for quick logging without needing to create an instance.

#### Example Usage

```csharp
using MetaLogger;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Set the log directory (optional)
        MetaLogger.SetLogDirectory("my_logs");

        // Optionally, enable debug mode and set a log file suffix.
        MetaLogger.EnableDebugMode(true);
        MetaLogger.SetLogFileSuffix("v1");

        // Log some messages.
        MetaLogger.LogInformation("Application started at {0}", DateTime.Now);
        MetaLogger.LogWarning("Low disk space on drive {0}", "C:");
        
        try
        {
            // Simulate an error.
            throw new Exception("Something went wrong!");
        }
        catch (Exception ex)
        {
            MetaLogger.LogError(ex, "An exception occurred while processing data.");
        }

        // Log a debug message.
        MetaLogger.LogDebug("This is a debug message.");
    }
}
```

### Instance Logger

The instance-based logger (`InstanceLogger`) allows you to have multiple loggers with different configurations. It also lets you set a custom message prefix and suffix for every log entry.

#### Example Usage

```csharp
using MetaLogger;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Create a new InstanceLogger
        InstanceLogger logger = new InstanceLogger();

        // Configure the logger
        logger.SetLogDirectory("instance_logs");
        logger.SetLogFileSuffix("build42");
        logger.EnableDebugMode(true);
        logger.SetMessagePrefix("[MyApp] ");
        logger.SetMessageSuffix(" ~EndLog");

        // Log some messages using the instance logger
        logger.LogInformation("Instance logger initialized at {0}", DateTime.Now);
        logger.LogWarning("This is a warning message.");
        
        try
        {
            // Simulate an error
            throw new InvalidOperationException("Invalid operation!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while executing operation.");
        }

        // Log a debug message.
        logger.LogDebug("This is a debug message from the instance logger.");
    }
}
```

## Configuration Options

Both loggers offer similar configuration settings:

- **Log Directory:**  
  Use `SetLogDirectory(string directory)` to specify where logs should be stored.

- **File Suffix:**  
  Use `SetLogFileSuffix(string suffix)` to add a suffix to the log file names (useful for versioning or timestamps).

- **Debug Mode:**  
  Enable or disable debug mode using `EnableDebugMode(bool enable)`. When enabled, debug messages are written to both the log file and the console.

- **Caller Information:**  
  Toggle the inclusion of caller information (e.g., file name, line number) in log messages using `SetIncludeCallerInfo(bool include)`.

- **Custom Message Prefix/Suffix (InstanceLogger only):**  
  Customize every log message by adding a prefix using `SetMessagePrefix(string prefix)` and a suffix using `SetMessageSuffix(string suffix)`.

## Building and Running

1. **Build the Project:**  
   Use your favorite IDE (such as Visual Studio) or the .NET CLI:

   ```bash
   dotnet build
   ```

2. **Run the Application:**  
   For example, using the .NET CLI:

   ```bash
   dotnet run --project YourProjectName
   ```

## License

This project is licensed under the [MIT License](LICENSE.md).