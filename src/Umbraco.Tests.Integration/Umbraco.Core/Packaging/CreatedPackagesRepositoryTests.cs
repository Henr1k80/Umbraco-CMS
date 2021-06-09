// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Tests.Integration.Umbraco.Core.Packaging
{
    [TestFixture]
    [UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
    public class CreatedPackagesRepositoryTests : UmbracoIntegrationTest
    {
        private Guid _testBaseFolder;

        [SetUp]
        public void SetupTestData() => _testBaseFolder = Guid.NewGuid();

        [TearDown]
        public void DeleteTestFolder() =>
            Directory.Delete(HostingEnvironment.MapPathContentRoot("~/" + _testBaseFolder), true);

        private IContentService ContentService => GetRequiredService<IContentService>();

        private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

        private IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();

        private IFileService FileService => GetRequiredService<IFileService>();

        private IMacroService MacroService => GetRequiredService<IMacroService>();

        private ILocalizationService LocalizationService => GetRequiredService<ILocalizationService>();

        private IEntityXmlSerializer EntityXmlSerializer => GetRequiredService<IEntityXmlSerializer>();

        private IHostingEnvironment HostingEnvironment => GetRequiredService<IHostingEnvironment>();

        private IUmbracoVersion UmbracoVersion => GetRequiredService<IUmbracoVersion>();

        private IMediaService MediaService => GetRequiredService<IMediaService>();

        private IMediaTypeService MediaTypeService => GetRequiredService<IMediaTypeService>();

        public ICreatedPackagesRepository PackageBuilder => new PackagesRepository(
            ContentService,
            ContentTypeService,
            DataTypeService,
            FileService,
            MacroService,
            LocalizationService,
            HostingEnvironment,
            EntityXmlSerializer,
            LoggerFactory,
            UmbracoVersion,
            Microsoft.Extensions.Options.Options.Create(new GlobalSettings()),
            MediaService,
            MediaTypeService,
            "createdPackages.config",

            // temp paths
            tempFolderPath: "~/" + _testBaseFolder + "/temp",
            packagesFolderPath: "~/" + _testBaseFolder + "/packages",
            mediaFolderPath: "~/" + _testBaseFolder + "/media");

        [Test]
        public void Delete()
        {
            var def1 = new PackageDefinition
            {
                Name = "test",
            };

            bool result = PackageBuilder.SavePackage(def1);
            Assert.IsTrue(result);

            PackageBuilder.Delete(def1.Id);

            def1 = PackageBuilder.GetById(def1.Id);
            Assert.IsNull(def1);
        }

        [Test]
        public void Create_New()
        {
            var def1 = new PackageDefinition
            {
                Name = "test",
            };

            bool result = PackageBuilder.SavePackage(def1);

            Assert.IsTrue(result);
            Assert.AreEqual(1, def1.Id);
            Assert.AreNotEqual(default(Guid).ToString(), def1.PackageId);

            var def2 = new PackageDefinition
            {
                Name = "test2",
            };

            result = PackageBuilder.SavePackage(def2);

            Assert.IsTrue(result);
            Assert.AreEqual(2, def2.Id);
            Assert.AreNotEqual(default(Guid).ToString(), def2.PackageId);
        }

        [Test]
        public void Update_Not_Found()
        {
            var def = new PackageDefinition
            {
                Id = 3, // doesn't exist
                Name = "test",
            };

            bool result = PackageBuilder.SavePackage(def);

            Assert.IsFalse(result);
        }

        [Test]
        public void Update()
        {
            var def = new PackageDefinition
            {
                Name = "test",
            };
            bool result = PackageBuilder.SavePackage(def);

            def.Name = "updated";
            result = PackageBuilder.SavePackage(def);
            Assert.IsTrue(result);

            // re-get
            def = PackageBuilder.GetById(def.Id);
            Assert.AreEqual("updated", def.Name);

            // TODO: There's a whole lot more assertions to be done
        }

        [Test]
        public void Export()
        {
            string file1 = $"~/{_testBaseFolder}/App_Plugins/MyPlugin/package.manifest";
            string file2 = $"~/{_testBaseFolder}/App_Plugins/MyPlugin/styles.css";
            string mappedFile1 = HostingEnvironment.MapPathContentRoot(file1);
            string mappedFile2 = HostingEnvironment.MapPathContentRoot(file2);
            Directory.CreateDirectory(Path.GetDirectoryName(mappedFile1));
            Directory.CreateDirectory(Path.GetDirectoryName(mappedFile2));
            File.WriteAllText(mappedFile1, "hello world");
            File.WriteAllText(mappedFile2, "hello world");

            var def = new PackageDefinition
            {
                Name = "test",
                Actions = "<actions><Action alias='test' /></actions>"
            };
            bool result = PackageBuilder.SavePackage(def);
            Assert.IsTrue(result);
            Assert.IsTrue(def.PackagePath.IsNullOrWhiteSpace());

            string zip = PackageBuilder.ExportPackage(def);

            def = PackageBuilder.GetById(def.Id); // re-get
            Assert.IsNotNull(def.PackagePath);

            using (ZipArchive archive = ZipFile.OpenRead(HostingEnvironment.MapPathWebRoot(zip)))
            {
                Assert.AreEqual(1, archive.Entries.Count);

                // the 2 files we manually added
                Assert.IsNotNull(archive.Entries.Where(x => x.Name == "package.manifest"));
                Assert.IsNotNull(archive.Entries.Where(x => x.Name == "styles.css"));

                // this is the actual package definition/manifest (not the developer manifest!)
                ZipArchiveEntry packageXml = archive.Entries.FirstOrDefault(x => x.Name == "package.xml");
                Assert.IsNotNull(packageXml);

                using (Stream stream = packageXml.Open())
                {
                    var xml = XDocument.Load(stream);
                    Assert.AreEqual("umbPackage", xml.Root.Name.ToString());

                    Assert.AreEqual("<Actions><Action alias=\"test\" /></Actions>", xml.Element("umbPackage").Element("Actions").ToString(SaveOptions.DisableFormatting));

                    // TODO: There's a whole lot more assertions to be done
                }
            }
        }
    }
}
