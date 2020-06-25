using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GitTfs.Core;
using System.Diagnostics;
using System.Linq;

namespace GitTfs.Util
{
    public class Mapping
    {
        public Mapping(string tfsPath, string localPath)
        {
            TfsPath = tfsPath;
            LocalPath = (localPath ?? string.Empty).TrimStart('/');
        }

        public string TfsPath { get; set; }
        public string LocalPath { get; set; }


        public string LocalPathWithRoot(string root)
        {
            return Path.Combine(root, LocalPath);
        }

        public string TfsPathWithRoot(string root)
        {
            return string.IsNullOrWhiteSpace(TfsPath) ? root.TrimEnd('/') : $"{root.TrimEnd('/')}/{TfsPath.TrimStart('/')}";
        }
    }

    [StructureMapSingleton]
    public class MappingsFile
    {
        private readonly List<Mapping> _mappings = new List<Mapping>();
        
        public MappingsFile()
        { }

        public bool IsParseSuccessfull { get; set; }

        public static string GitTfsCachedMappingFileName = "git-tfs_mappings";

        public List<Mapping> Mappings => _mappings;


        public bool Parse(string filePath)
        {
            try
            {
                _mappings.Clear();

                var mappings = File.ReadAllLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line =>
                {
                    var paths = line.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                    return new Mapping(paths[0],  paths.Count >= 2 ? paths[1] : string.Empty);
                });

                _mappings.AddRange(mappings);
            }
            catch (Exception e)
            {
                throw new GitTfsException($"Unable to parse mapping file {filePath}",e);
            }
            
            IsParseSuccessfull = true;
            return true;
        }

        public bool Parse(string filePath, string gitDir, bool couldSaveAuthorFile)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return LoadMappingsFromSavedFile(gitDir);
            }

            if (!File.Exists(filePath))
            {
                throw new GitTfsException("Mappings file cannot be found: '" + filePath + "'");
            }

            if (couldSaveAuthorFile)
            {
                SaveMappingFileInRepository(filePath, gitDir);
            }

            Trace.WriteLine("Reading mappings file : " + filePath);
            return Parse(filePath);

        }

        private string GetSavedMappingFilePath(string gitDir)
        {
            return Path.Combine(gitDir, GitTfsCachedMappingFileName);
        }
        
        public void SaveMappingFileInRepository(string filePath, string gitDir)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                return;

            var savedMappingFilePath = GetSavedMappingFilePath(gitDir);
            try
            {
                File.Copy(filePath, savedMappingFilePath, true);
            }
            catch (Exception)
            {
                Trace.TraceWarning("Failed to copy authors file from \"" + filePath + "\" to \"" +
                                   savedMappingFilePath + "\".");
            }
        }

        public bool LoadMappingsFromSavedFile(string gitDir)
        {
            var savedMappingFilePath = GetSavedMappingFilePath(gitDir);
            if (!File.Exists(savedMappingFilePath))
            {
                Trace.WriteLine("No mappings file used.");
                return false;
            }

            if (Mappings.Count != 0)
                return true;
            Trace.WriteLine("Reading cached mappings file (" + savedMappingFilePath + ")...");
            return Parse(savedMappingFilePath);
        }

       

    }

    [StructureMapSingleton]
    public class ExcludedRenamesFile
    {
        private readonly List<int> _list = new List<int>();

        public ExcludedRenamesFile()
        { }

        public bool IsParseSuccessfull { get; set; }

        public static string GitTfsCachedMappingFileName = "git-tfs_excluded_renames";

        public List<int> List => _list;


        public bool Parse(string filePath)
        {
            try
            {
                _list.Clear();
                var list = File.ReadAllLines(filePath)
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Select(line=>line.Trim())
                                .Where(line =>
                                {
                                    int result = 0;
                                    return int.TryParse(line, out result);
                                })
                                .Select(int.Parse);
                _list.AddRange(list);
            }
            catch (Exception e)
            {
                throw new GitTfsException($"Unable to parse excluded renames file {filePath}", e);
            }

            IsParseSuccessfull = true;
            return true;
        }

        public bool Parse(string filePath, string gitDir, bool couldSave)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return LoadExcludedRenamesFromSavedFile(gitDir);
            }

            if (!File.Exists(filePath))
            {
                throw new GitTfsException("Excluded renames file cannot be found: '" + filePath + "'");
            }

            if (couldSave)
            {
                SaveExcludedRenamesFileInRepository(filePath, gitDir);
            }

            Trace.WriteLine("Reading Excluded renames file : " + filePath);
            return Parse(filePath);

        }

        private string GetSavedExcludedRenamesFilePath(string gitDir)
        {
            return Path.Combine(gitDir, GitTfsCachedMappingFileName);
        }

        public void SaveExcludedRenamesFileInRepository(string filePath, string gitDir)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var savedExcludedRenamesFilePath = GetSavedExcludedRenamesFilePath(gitDir);
            try
            {
                File.Copy(filePath, savedExcludedRenamesFilePath, true);
            }
            catch (Exception)
            {
                Trace.TraceWarning("Failed to copy Excluded renames file from \"" + filePath + "\" to \"" +
                                   savedExcludedRenamesFilePath + "\".");
            }
        }

        public bool LoadExcludedRenamesFromSavedFile(string gitDir)
        {
            var savedExcludedRenamesFilePath = GetSavedExcludedRenamesFilePath(gitDir);
            if (!File.Exists(savedExcludedRenamesFilePath))
            {
                Trace.WriteLine("No Excluded renames file used.");
                return false;
            }

            if (List.Count != 0)
                return true;
            Trace.WriteLine("Reading cached Excluded renames file (" + savedExcludedRenamesFilePath + ")...");
            return Parse(savedExcludedRenamesFilePath);
        }



    }
}
