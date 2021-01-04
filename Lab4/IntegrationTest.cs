using System;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Text;
using IIG.BinaryFlag;
using IIG.CoSFE.DatabaseUtils;
using IIG.DatabaseConnectionUtils;
using IIG.FileWorker;
using Xunit;

namespace IIG.IntegrationTest
{
    public class IntegrationTest
    {
        private static NameValueCollection appSettingsStorage = new NameValueCollection()
        {
            {"Database_ServerPath", "localhost"},
            {"Database_DatabaseName", "IIG.CoSWE.StorageDB"},
            {"Database_TrustedConnection", "false"},
            {"Database_UserLogin", "sa"},
            {"Database_UserPassword", "210800.daniil"},
            {"Database_ConnectionTimeout", "75"}
        };
        private static NameValueCollection appSettingsFlag = new NameValueCollection()
        {
            {"Database_ServerPath", "localhost"},
            {"Database_DatabaseName", "IIG.CoSWE.FlagPoleDB"},
            {"Database_TrustedConnection", "false"},
            {"Database_UserLogin", "sa"},
            {"Database_UserPassword", "210800.daniil"},
            {"Database_ConnectionTimeout", "75"}
        };

        private StorageDatabaseUtils storageDB = new StorageDatabaseUtils(appSettingsStorage);
        private FlagpoleDatabaseUtils flagPoleDB = new FlagpoleDatabaseUtils(appSettingsFlag);
        
        [Fact]
        public void CheckIfConnectionSucceed()
        {
            var connection = new DatabaseConnection(appSettingsStorage).ExecSql("select * from Files");
            Assert.True(connection);
        }
        
        [Theory]
        [InlineData(new object[]
        {
            new string[]
            {
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile1.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile2.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile3.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile4.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile5.txt",
            }
        })]
        public void ReadFile_WriteToDB_ReadFromDB_setFlag( string[] files)
        {
            var mbf = new MultipleBinaryFlag((ulong) files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                var fileName = BaseFileWorker.GetFileName(files[i]);
                var fileContent = Encoding.ASCII.GetBytes(BaseFileWorker.ReadAll(files[i]));
                Assert.EndsWith(".txt", fileName);
                storageDB.AddFile(fileName, fileContent);
                mbf.SetFlag((ulong)i);
                string fileNameCheck;
                byte[] fileContentCheck;
                storageDB.GetFile(i+1 , out fileNameCheck, out fileContentCheck);
                Assert.Equal(fileContentCheck, fileContent );
                Assert.Equal(fileNameCheck, fileName );
            }
            string flagView = mbf.ToString();
            bool flagValue = mbf.GetFlag();
            flagPoleDB.AddFlag(flagView, flagValue);
            string flagViewCheck;
            Nullable<bool> flagValueCheck;
            flagPoleDB.GetFlag(1, out flagViewCheck, out flagValueCheck);
            Assert.Equal(flagValueCheck, flagValue);
            Assert.Equal(flagViewCheck, flagView);
            new DatabaseConnection(appSettingsStorage).ExecSql("use [IIG.CoSWE.StorageDB];delete from Files; DBCC CHECKIDENT (Files, RESEED, 0);");
            new DatabaseConnection(appSettingsStorage).ExecSql("use [IIG.CoSWE.FlagPoleDB];delete from MultipleBinaryFlags; DBCC CHECKIDENT (MultipleBinaryFlags, RESEED, 0);");
        }

        [Theory]
        [InlineData(new object[]
        {
            new string[]
            {
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile1.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile2.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile3.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile4.txt",
                "/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/testData/NewFile5.txt",
            }
        })]
        public void ReadFromDb_WriteToDir_setFlag_WriteFlagToDB(string[] files)
        {
            var mbf = new MultipleBinaryFlag((ulong) files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                var fileName = BaseFileWorker.GetFileName(files[i]);
                var fileContent = Encoding.ASCII.GetBytes(BaseFileWorker.ReadAll(files[i]));
                storageDB.AddFile(fileName, fileContent);
            }

            var testDir = BaseFileWorker.MkDir("/home/daniildenisyuk/projects/KPI_Labs/CSE/Lab4/test");
            
            var filesRows = storageDB.GetFiles().Rows;
            ulong n = 0;
            foreach(DataRow fileRow in filesRows)
            {
                BaseFileWorker.Write(fileRow[2].ToString(), Path.Combine(testDir, fileRow[1].ToString()));
                Assert.NotEmpty(BaseFileWorker.GetPath(Path.Combine(testDir, fileRow[1].ToString())));
                mbf.SetFlag(n);
                n++;
            }
            string flagView = mbf.ToString();
            bool flagValue = mbf.GetFlag();
            flagPoleDB.AddFlag(flagView, flagValue);
            string flagViewCheck;
            Nullable<bool> flagValueCheck;
            flagPoleDB.GetFlag(1, out flagViewCheck, out flagValueCheck); 
            Assert.Equal(flagValueCheck, flagValue);
            Assert.Equal(flagViewCheck, flagView);
            new DatabaseConnection(appSettingsStorage).ExecSql("use [IIG.CoSWE.StorageDB];delete from Files; DBCC CHECKIDENT (Files, RESEED, 0);");
            new DatabaseConnection(appSettingsStorage).ExecSql("use [IIG.CoSWE.FlagPoleDB];delete from MultipleBinaryFlags; DBCC CHECKIDENT (MultipleBinaryFlags, RESEED, 0);");
        }
    }
}