using System.Diagnostics;
using System.IO;

public class End2End
{
    [Fact]
    public void TestReadCheep()
    {
        // Arrange
        ArrangeTestDatabase();
        // Act
        string output = "";
        using (var process = new Process())
        {
            process.StartInfo.FileName = "/usr/local/share/dotnet/dotnet";
            process.StartInfo.Arguments = "/Users/andersgrangaard/Chirp.CLI/src/Chirp.CLI.Client/bin/Debug/net8.0/Chirp.CLI.client.dll read 10";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = "/Users/andersgrangaard/Chirp.CLI";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            // Synchronously read the standard output of the spawned process.
            StreamReader reader = process.StandardOutput;
            StreamReader errorReader = process.StandardError;
            output = reader.ReadToEnd();
            string errorOutput = errorReader.ReadToEnd();
            process.WaitForExit();
            
            // If there's an error, include it in the output for debugging
            if (!string.IsNullOrEmpty(errorOutput))
            {
                output = errorOutput + "\n" + output;
            }
        }
        string fstCheep = output.Split("\n")[0];
        // Assert
        Assert.StartsWith("ropf", fstCheep);
        Assert.EndsWith("Hello, World!", fstCheep);
    }

    private void ArrangeTestDatabase()
    {
        // Create test CSV file with expected data
        // The working directory when the CLI runs will be the project root
        string dbFilePath = "/Users/andersgrangaard/Chirp.CLI/chirp_cli_db.csv";
        
        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(dbFilePath)!);
        
        // Create CSV content with header and test data
        // The test expects "ropf" to start and "Hello, World!" to end the first line
        string csvContent = "Author,Message,Timestamp\n" +
                           "ropf,\"Hello, World!\",1640995200"; // Unix timestamp for a fixed date
        
        File.WriteAllText(dbFilePath, csvContent);
    }
}
