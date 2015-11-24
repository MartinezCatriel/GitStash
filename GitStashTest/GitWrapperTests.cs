﻿using System;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GitWrapper;
using System.Collections.Generic;

namespace GitStash
{
    [TestClass]
    public class GitWrapperTests
    {
        [TestInitialize]
        public void Setup()
        {
            if (Directory.Exists("test"))
                Directory.Delete("test", true);

            ZipFile.ExtractToDirectory("test.zip", ".");
        }

        [TestCleanup]
        public void TearDown()
        {
            var rootInfo = new DirectoryInfo("test") { Attributes = FileAttributes.Normal };
            foreach (var fileInfo in rootInfo.GetFileSystemInfos()) fileInfo.Attributes = FileAttributes.Normal;
            foreach (var subDirectory in Directory.GetDirectories("test", "*", SearchOption.AllDirectories))
            {
                var subInfo = new DirectoryInfo(subDirectory) { Attributes = FileAttributes.Normal };
                foreach (var fileInfo in subInfo.GetFileSystemInfos()) fileInfo.Attributes = FileAttributes.Normal;
            }
            Directory.Delete("test", true);
        }

        [TestMethod]
        public void TestStash()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();
            GitStashWrapper git = new GitStashWrapper("test");
            IEnumerable<IGitStash> stashes = git.Stashes;
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);

            Assert.IsFalse(File.Exists("file2"));
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
        }

        [TestMethod]
        public void TestDropStash()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();
            GitStashWrapper git = new GitStashWrapper("test");
            IEnumerable<IGitStash> stashes = git.Stashes;
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);

            results = git.DropStash(new GitStashOptions(), 0);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 0);
        }

        [TestMethod]
        public void TestPopStash()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();
            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file2"));

            results = git.PopStash(new GitStashOptions(), 0);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 0);
            Assert.IsTrue(File.Exists(@"test\file2"));
        }

        [TestMethod]
        public void TestApplyStash()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();
            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file2"));

            results = git.ApplyStash(new GitStashOptions(), 0);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsTrue(File.Exists(@"test\file2"));
        }

        [TestMethod]
        public void StashWontPopIfConflictedOnStagedFile()
        {
            File.WriteAllText(@"test\file1", "This is a test");

            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file1"));

            using (StreamWriter sw = File.AppendText(@"test\file1"))
            {
                sw.WriteLine("This is another test");
            }

            results = git.PopStash(new GitStashOptions(), 0);
            Assert.IsFalse(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            string txt = File.ReadAllText(@"test\file1");
            Assert.IsTrue(txt == "This is a test\r\nThis is another test\r\n");
        }

        [TestMethod]
        public void StashWontApplyIfConflictedOnStagedFile()
        {
            File.WriteAllText(@"test\file1", "This is a test");

            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file1"));

            using (StreamWriter sw = File.AppendText(@"test\file1"))
            {
                sw.WriteLine("This is another test");
            }

            results = git.ApplyStash(new GitStashOptions(), 0);
            Assert.IsFalse(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            string txt = File.ReadAllText(@"test\file1");
            Assert.IsTrue(txt == "This is a test\r\nThis is another test\r\n");

        }

        [TestMethod]
        public void StashWontPopIfConflictedOnUnStagedFile()
        {
            File.WriteAllText(@"test\file1", "This is a test");

            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file1"));

            using (StreamWriter sw = File.AppendText(@"test\file1"))
            {
                sw.WriteLine("This is another test");
            }

            results = git.PopStash(new GitStashOptions(), 0);
            Assert.IsFalse(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            string txt = File.ReadAllText(@"test\file1");
            Assert.IsTrue(txt == "This is a test\r\nThis is another test\r\n");
        }

        [TestMethod]
        [ExpectedException(typeof(GitStashInvalidIndexException))]
        public void TestPopthrowsExceptionWithInvalidIndex()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();
            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file2"));

            results = git.PopStash(new GitStashOptions(), -1);
            Assert.IsFalse(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists(@"test\file2"));
        }

        [TestMethod]
        [ExpectedException(typeof(GitStashInvalidIndexException))]
        public void TestApplythrowsExceptionWithInvalidIndex()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();
            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file2"));

            results = git.ApplyStash(new GitStashOptions(), 2);
            Assert.IsFalse(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists(@"test\file2"));
        }

        [TestMethod]
        [ExpectedException(typeof(GitStashInvalidIndexException))]
        public void TestDeletethrowsExceptionWithInvalidIndex()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();
            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "Testing";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file2"));

            results = git.DropStash(new GitStashOptions(), 2);
            Assert.IsFalse(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists(@"test\file2"));
        }

        [TestMethod]
        public void TestStashesReturnsTheProperIndex()
        {
            FileStream fs = File.Create(@"test\file2");
            fs.Close();

            GitStashWrapper git = new GitStashWrapper("test");
            GitStashOptions options = new GitStashOptions { Untracked = true };
            options.Message = "one";
            IGitStashResults results = git.SaveStash(options);
            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 1);
            Assert.IsFalse(File.Exists("file2"));

            fs = File.Create(@"test\file2");
            fs.Close();

            options.Message = "two";
            results = git.SaveStash(options);

            Assert.IsTrue(results.Success);
            Assert.IsTrue(git.Stashes.Count == 2);
            Assert.IsFalse(File.Exists("file2"));

            IGitStash recent = git.Stashes[0];
            IGitStash older = git.Stashes[1];

            Assert.IsTrue(recent.Index == 0);
            Assert.IsTrue(recent.Message == "two");

            Assert.IsTrue(older.Index == 1);
            Assert.IsTrue(older.Message == "one");
        }
    }
}