using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Test;
using Xunit;

namespace IxMilia.Dxf.Integration.Test
{
    public abstract class CompatTestsBase : AbstractDxfTests
    {
        public static readonly string MinimumFileText = @"
  0
SECTION
  2
ENTITIES
  0
LINE
  8
0
 10
0.0
 20
0.0
 30
0.0
 11
10.0
 21
10.0
 31
0.0
  0
ENDSEC
  0
EOF
".Trim();

        public class ManageTemporaryDirectory : IDisposable
        {
            public string DirectoryPath { get; }

            public ManageTemporaryDirectory()
            {
                DirectoryPath = Path.Combine(
                    Path.GetTempPath(),
                    Guid.NewGuid().ToString()
                    );
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, true);
                }

                Directory.CreateDirectory(DirectoryPath);
            }

            public void Dispose()
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, true);
                }
            }
        }

        protected void RoundTripDimensionWithXData(Func<DxfFile, DxfFile> roundTripper)
        {
            var dim = new DxfAlignedDimension();
            dim.XData.Add("ACAD",
                new DxfXDataApplicationItemCollection(
                    new DxfXDataString("DSTYLE"),
                    new DxfXDataItemList(
                        new DxfXDataItem[]
                        {
                            new DxfXDataInteger(271),
                            new DxfXDataInteger(9),
                        })
                ));
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.Entities.Add(dim);

            // perform round trip
            var result = roundTripper(file);

            // verify
            var roundTrippedDim = (DxfAlignedDimension)result.Entities.Single();
            var xdataPair = roundTrippedDim.XData.Single();
            Assert.Equal("ACAD", xdataPair.Key);

            var styleItems = xdataPair.Value;
            Assert.Equal("DSTYLE", ((DxfXDataString)styleItems.First()).Value);
            var dataItems = (DxfXDataItemList)styleItems.Last();
            Assert.Single(dataItems.Items.OfType<DxfXDataInteger>().Where(i => i.Value == 271));
        }

        protected void WaitForProcess(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = fileName;
            psi.Arguments = arguments;
            var proc = Process.Start(psi);
            proc.WaitForExit();
            Assert.Equal(0, proc.ExitCode);
        }
    }
}
