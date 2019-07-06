using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SwiftPbo.Tests
{
    [TestFixture]
    class PboTest
    {
        private byte[] _checksum;
        private string _testdata;
        [SetUp]
        protected void SetUp()
        {

            const string Sha = "2DEA9A198FDCF0FE70473C079F1036B6E16FBFCE";
            _checksum = Enumerable.Range(0, Sha.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(Sha.Substring(x, 2), 16))
                .ToArray();

            _testdata = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "SwiftPbo.Tests", "testdata");
        }

        [Test]
        public void OpenArchiveTest()
        {
            string pathToFile = System.IO.Path.Combine(_testdata, "cba_common.pbo");
            var pboArchive = new PboArchive(pathToFile);
            Assert.That(pboArchive.Files.Count == 113);

            Assert.That(pboArchive.Checksum.SequenceEqual(_checksum), "Checksum dosen't match");
            Assert.That(pboArchive.ProductEntry.Name == "prefix");
            Assert.That(pboArchive.ProductEntry.Prefix == @"x\cba\addons\common");
            Assert.That(pboArchive.ProductEntry.Addtional.Count == 3);
        }

        [Test]
        public void CreateArchiveTest()
        {
            string pathToTestMission = System.IO.Path.Combine(_testdata, "cba_common");
            string pathToNewPbo = System.IO.Path.Combine(_testdata, "out", "new_cba_common.pbo");
            Assert.That(PboArchive.Create(pathToTestMission, pathToNewPbo));

            var pbo = new PboArchive(pathToNewPbo);
            Assert.That(pbo.Files.Count == 113);

            // checksums shoulden't match due to the time.
            Assert.False(pbo.Checksum.SequenceEqual(_checksum), "Checksum match");
            Assert.That(pbo.ProductEntry.Name == "prefix");
            Assert.That(pbo.ProductEntry.Prefix == @"x\cba\addons\common");
            Assert.That(pbo.ProductEntry.Addtional.Count == 1); // i don't add wonky shit like mikero.
        }

        //[Test]
        public void CloneArchiveTest()
        {
            string pathToFile = System.IO.Path.Combine(_testdata, "cba_common.pbo");
            string pathToClonedPbo = System.IO.Path.Combine(_testdata, "out", "cloned_cba_common.pbo");

            var pboArchive = new PboArchive(pathToFile);
            var files = new Dictionary<FileEntry, string>();

            foreach (var entry in pboArchive.Files)
            {
                var info = new FileInfo(System.IO.Path.Combine(_testdata, "cba_common", entry.FileName));
                Assert.That(info.Exists);
                files.Add(entry, info.FullName);
            }

            PboArchive.Clone(pathToClonedPbo, pboArchive.ProductEntry, files, pboArchive.Checksum);
            var cloneArchive = new PboArchive(pathToClonedPbo);
            //Assert.That(pboArchive.Checksum.SequenceEqual(cloneArchive.Checksum), "Checksum dosen't match");
            Assert.AreEqual(pboArchive.Checksum, cloneArchive.Checksum);
            Assert.That(pboArchive.Files.Count == cloneArchive.Files.Count, "Files count dosen't match");
            Assert.That(pboArchive.ProductEntry.Name == cloneArchive.ProductEntry.Name);
            Assert.That(pboArchive.ProductEntry.Prefix == cloneArchive.ProductEntry.Prefix);
            Assert.That(pboArchive.ProductEntry.Addtional.Count == cloneArchive.ProductEntry.Addtional.Count);
        }
    }


    [TestFixture]
    class FilterFiles
    {
        private string _testdataPath;

        [SetUp]
        public void Setup()
        {
            _testdataPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "SwiftPbo.Tests", "testdata");
        }

        [Test]
        public void GetFilesExcludesExtension()
        {
            // Arrange
            string pathToTestMission = System.IO.Path.Combine(_testdataPath, "FilterTestFiles");
            Config.FilterFileConfig config = new Config.FilterFileConfig();
            config.ExcludedSubstringInPath = new string[] {};
            config.ExcludedExtensions = new string[] { ".tproj" };
            config.ExcludeAllHidden = false;

            string[] expectedFiles = {
                System.IO.Path.Combine(pathToTestMission, "aFile.sqx"),
                System.IO.Path.Combine(pathToTestMission, "aFile.sqf"),
                System.IO.Path.Combine(pathToTestMission, "aFile_Hidden.sqf"),
                System.IO.Path.Combine(pathToTestMission, "Hidden", "aFile.sqf"),
                //Path.Combine(pathToTestMission, "project.tproj"),
                System.IO.Path.Combine(pathToTestMission, "CPack.Config"),
                System.IO.Path.Combine(pathToTestMission, "Tests", "aFile.sqf"),
                System.IO.Path.Combine(pathToTestMission, ".gitignore"),
                System.IO.Path.Combine(pathToTestMission, ".git", "aFile.sqf"),
            };

            // Act
            string[] files = new PboArchive().GetFiles(pathToTestMission, config);

            // Assert
            CollectionAssert.AreEquivalent(expectedFiles, files);
            Assert.AreEqual(expectedFiles.Length, files.Length);
        }

        [Test]
        public void GetFilesExcludesFileNames()
        {
            // Arrange
            string pathToTestMission = System.IO.Path.Combine(_testdataPath, "FilterTestFiles");
            Config.FilterFileConfig config = new Config.FilterFileConfig();
            config.ExcludedSubstringInPath = new string[] { "CPack.Config" };
            config.ExcludedExtensions = new string[] { };
            config.ExcludeAllHidden = false;

            string[] expectedFiles = {
                System.IO.Path.Combine(pathToTestMission, "aFile.sqx"),
                System.IO.Path.Combine(pathToTestMission, "aFile.sqf"),
                System.IO.Path.Combine(pathToTestMission, "aFile_Hidden.sqf"),
                System.IO.Path.Combine(pathToTestMission, "Hidden", "aFile.sqf"),
                System.IO.Path.Combine(pathToTestMission, "project.tproj"),
                //Path.Combine(pathToTestMission, "CPack.Config"),
                System.IO.Path.Combine(pathToTestMission, "Tests", "aFile.sqf"),
                System.IO.Path.Combine(pathToTestMission, ".gitignore"),
                System.IO.Path.Combine(pathToTestMission, ".git", "aFile.sqf"),
            };

            // Act
            string[] files = new PboArchive().GetFiles(pathToTestMission, config);

            // Assert
            CollectionAssert.AreEquivalent(expectedFiles, files);
            Assert.AreEqual(expectedFiles.Length, files.Length);
        }

        [Test]
        public void GetFilesExcludesHidden()
        {
            // Arrange
            string pathToTestMission = System.IO.Path.Combine(_testdataPath, "FilterTestFiles");
            Config.FilterFileConfig config = new Config.FilterFileConfig();
            config.ExcludedSubstringInPath = new string[] { };
            config.ExcludedExtensions = new string[] { };
            config.ExcludeAllHidden = true;

            string[] expectedFiles = {
                System.IO.Path.Combine(pathToTestMission, "aFile.sqx"),
                System.IO.Path.Combine(pathToTestMission, "aFile.sqf"),
                //Path.Combine(pathToTestMission, "aFile_Hidden.sqf"),
                //Path.Combine(pathToTestMission, "Hidden", "aFile.sqf"),
                System.IO.Path.Combine(pathToTestMission, "project.tproj"),
                System.IO.Path.Combine(pathToTestMission, "CPack.Config"),
                System.IO.Path.Combine(pathToTestMission, "Tests", "aFile.sqf"),
                System.IO.Path.Combine(pathToTestMission, ".gitignore"),
                System.IO.Path.Combine(pathToTestMission, ".git", "aFile.sqf"),
            };

            // Act
            string[] files = new PboArchive().GetFiles(pathToTestMission, config);

            // Assert
            CollectionAssert.AreEquivalent(expectedFiles, files);
            Assert.AreEqual(expectedFiles.Length, files.Length);
        }

        [Test]
        public void GetFilesExcludesDefault()
        {
            // Arrange
            string pathToTestMission = System.IO.Path.Combine(_testdataPath, "FilterTestFiles");
            
            string[] expectedFiles = {
                //Path.Combine(pathToTestMission, "aFile.sqx"),
                System.IO.Path.Combine(pathToTestMission, "aFile.sqf"),
                //Path.Combine(pathToTestMission, "aFile_Hidden.sqf"),
                //Path.Combine(pathToTestMission, "Hidden", "aFile.sqf"),
                //Path.Combine(pathToTestMission, "project.tproj"),
                //Path.Combine(pathToTestMission, "CPack.Config"),
                System.IO.Path.Combine(pathToTestMission, "Tests", "aFile.sqf"),
                //Path.Combine(pathToTestMission, ".gitignore"),
                //Path.Combine(pathToTestMission, ".git", "aFile.sqf"),
            };

            // Act
            string[] files = new PboArchive().GetFiles(pathToTestMission);

            // Assert
            CollectionAssert.AreEquivalent(expectedFiles, files);
            Assert.AreEqual(expectedFiles.Length, files.Length);
        }
    }
}
