using System.Diagnostics;

namespace FilesCompiler
{
    internal class GitConfigure
    {   /// <summary>
        /// Helper method that runs a Git command in the specified working directory.
        /// Throws an exception if the Git command fails.
        /// </summary>
        private static string RunGitCommand(string command, string workingDirectory)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Git command failed: {command}\nError: {error}");
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        /// Commits and pushes a batch of files.
        /// </summary>
        private static void CommitAndPushBatch(string repoDirectory, List<string> relativeFilePathsInBatch, int batchNumber, string branchToPush)
        {
            if (relativeFilePathsInBatch == null || !relativeFilePathsInBatch.Any())
            {
                Console.WriteLine($"Skipping empty batch {batchNumber}.");
                return;
            }

            Console.WriteLine($"Processing Batch {batchNumber} ({relativeFilePathsInBatch.Count} files)...");

            // Stage files for commit using relative paths
            // Quote paths to handle spaces and special characters
            // Use "--" to separate options from file paths, preventing issues if a path looks like an option
            string filesToAddArgument = string.Join(" ", relativeFilePathsInBatch.Select(p => $"\"{p.Replace("\"", "\\\"")}\"")); // Basic escaping for quotes
            RunGitCommand($"add -- {filesToAddArgument}", repoDirectory);
            Console.WriteLine($"Staged {relativeFilePathsInBatch.Count} files for batch {batchNumber}.");

            // Commit the staged files
            string commitMessage = $"Batch commit #{batchNumber} with {relativeFilePathsInBatch.Count} file(s) at {DateTime.UtcNow.ToString("HH:mm:ss")}";
            // Escape quotes in commit message itself if necessary (less common)
            RunGitCommand($"commit -m \"{commitMessage.Replace("\"", "\\\"")}\"", repoDirectory);
            Console.WriteLine($"Committed batch {batchNumber}.");

            // Push the commit to the remote temporary branch
            // No -u needed here; we set upstream only on the final push.
            RunGitCommand($"push origin {branchToPush}", repoDirectory);
            Console.WriteLine($"Pushed batch {batchNumber} to remote branch {branchToPush}.");
        }

        /// <summary>
        /// Resets a local directory to a clean Git repository, commits all files in batches,
        /// and force-pushes to a specified remote URL and branch, overwriting remote history.
        /// </summary>
        /// <param name="repoDirectory">The absolute path to the local repository directory.</param>
        /// <param name="remoteUrl">The URL of the remote Git repository (e.g., GitHub URL).</param>
        /// <param name="finalBranchName">The desired name for the main branch on the remote (e.g., "master" or "main"). Defaults to "master".</param>

        private const string TempBranchName = "temp-initial-commit";

        //Ai generated im lazy to do it
        private const int BatchSizeLimitBytes = 99 * 1024 * 1024 * 5;
        public static void SetupAndPushNewRepoHistory(string repoDirectory, string remoteUrl, string finalBranchName = "main")
        {

            // --- 1. Validation and Preparation ---
            if (!Directory.Exists(repoDirectory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {repoDirectory}");
            }
            Console.WriteLine($"Starting repository reset and push for: {repoDirectory}");

            string gitFolder = Path.Combine(repoDirectory, ".git");

            /*try
            {
                if (Directory.Exists(gitFolder))
                {
                    // Force removal of the .git directory to ensure a clean state
                    Console.WriteLine("Removing existing .git folder...");
                    Directory.Delete(gitFolder, true);
                    // Short pause might help if file system locks are slow, but usually not needed
                    // System.Threading.Thread.Sleep(100);
                    Console.WriteLine("Removed existing .git folder.");
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to delete existing .git directory: {ex.Message}", ex);
            }*/

            // --- 2. Initialize New Repository ---
            RunGitCommand("init", repoDirectory);
            Console.WriteLine("Initialized new empty Git repository.");

            // Optional: Configure settings like compression. May help with large initial pushes.
            RunGitCommand("config core.compression 0", repoDirectory);
            RunGitCommand("config core.autocrlf false", repoDirectory); // Often good for cross-platform consistency

            // --- 3. Add Remote ---
            // Remove existing remote 'origin' if it somehow exists (belt-and-suspenders)
            try { RunGitCommand("remote remove origin", repoDirectory); } catch { /* Ignore if remote doesn't exist */ }
            RunGitCommand($"remote add origin {remoteUrl}", repoDirectory);
            Console.WriteLine($"Added remote 'origin': {remoteUrl}");

            // --- 4. Create Initial Orphan Branch ---
            // Always start on a temporary orphan branch to ensure no prior history is linked.
       
            try
            {
                RunGitCommand($"checkout --orphan {TempBranchName}", repoDirectory);
                Console.WriteLine($"Created and switched to temporary orphan branch: {TempBranchName}");
            }
            catch { }

            // --- 5. File Batching and Committing ---
            Console.WriteLine("Scanning files for batching...");
            // Get all files, excluding anything inside the .git directory
            string[] allFiles = Directory.GetFiles(repoDirectory, "*", SearchOption.AllDirectories)
                                         .Where(f => !f.Replace(Path.DirectorySeparatorChar, '/').StartsWith($"{gitFolder.Replace(Path.DirectorySeparatorChar, '/')}/", StringComparison.OrdinalIgnoreCase))
                                         .ToArray();

            Console.WriteLine($"Found {allFiles.Length} files to process.");

            List<string> currentBatchRelativePaths = new List<string>();
            long currentBatchSizeBytes = 0;
            int batchNumber = 1;

            foreach (string absoluteFilePath in allFiles)
            {
                // Ensure we use relative paths for git commands within the repo context
                string relativeFilePath = Path.GetRelativePath(repoDirectory, absoluteFilePath)
                                              .Replace(Path.DirectorySeparatorChar, '/'); // Use forward slashes for Git compatibility

                FileInfo fileInfo;
                try
                {
                    fileInfo = new FileInfo(absoluteFilePath);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"Warning: File not found during processing, skipping: {absoluteFilePath}");
                    continue; // Skip if file disappeared between listing and processing
                }

                long fileSize = fileInfo.Length;

                // Check if adding this file exceeds the limit *and* the current batch isn't empty
                if (currentBatchSizeBytes > 0 && currentBatchSizeBytes + fileSize > BatchSizeLimitBytes || string.Join("\"  \"", currentBatchRelativePaths).Length >= 7000)
                {
                    // Commit the current batch before starting a new one
                    CommitAndPushBatch(repoDirectory, currentBatchRelativePaths, batchNumber++, TempBranchName);
                    currentBatchRelativePaths.Clear();
                    currentBatchSizeBytes = 0;
                }

                // Handle files that are individually too large (commit them in their own batch)
                if (fileSize > BatchSizeLimitBytes)
                {
                    Console.WriteLine($"Warning: File '{relativeFilePath}' ({fileSize / 1024 / 1024} MB) exceeds batch size limit ({BatchSizeLimitBytes / 1024 / 1024} MB). Committing individually.");
                    // If there were files already in the current batch, commit them first
                    if (currentBatchRelativePaths.Any())
                    {
                        CommitAndPushBatch(repoDirectory, currentBatchRelativePaths, batchNumber++, TempBranchName);
                        currentBatchRelativePaths.Clear();
                        currentBatchSizeBytes = 0;
                    }
                    // Commit the large file by itself
                    CommitAndPushBatch(repoDirectory, new List<string> { relativeFilePath }, batchNumber++, TempBranchName);
                }
                else
                {
                    // Add the file to the current batch
                    currentBatchRelativePaths.Add(relativeFilePath);
                    currentBatchSizeBytes += fileSize;
                }
            }

            // Commit any remaining files in the last batch
            if (currentBatchRelativePaths.Any())
            {
                CommitAndPushBatch(repoDirectory, currentBatchRelativePaths, batchNumber, TempBranchName);
            }

            Console.WriteLine("Finished committing and pushing all file batches.");

            // --- 6. Finalize Branch and Force Push ---
            Console.WriteLine($"Renaming temporary branch '{TempBranchName}' to final branch '{finalBranchName}'...");
            // Rename the local temporary branch to the final desired name
            RunGitCommand($"branch -M {finalBranchName}", repoDirectory);
            Console.WriteLine($"Local branch renamed to '{finalBranchName}'.");

            Console.WriteLine($"Force pushing final branch '{finalBranchName}' to remote 'origin'...");
            // Force push the newly named local branch to the remote.
            // This overwrites the remote branch if it exists.
            // -u sets the upstream tracking relationship.
            RunGitCommand($"push -u origin {finalBranchName} --force", repoDirectory);
            Console.WriteLine($"Successfully force-pushed '{finalBranchName}' to remote.");

            //RunGitCommand($"push origin --delete {TempBranchName}", repoDirectory);
            //Console.WriteLine($"Successfully removed '{TempBranchName}' to remote. Repository setup complete.");

        }
    }
}
