using Microsoft.EntityFrameworkCore.Storage;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace TandemBooking.Services
{
    public class VippsService
    {
        private readonly VippsSettings _settings;
        private ConnectionInfo _connectionInfo;

        public VippsService(VippsSettings settings)
        {
            _settings = settings;

            var privateKeyBytes = Convert.FromBase64String(_settings.SftpPrivateKey);
            var privateKey = new PrivateKeyFile(new MemoryStream(privateKeyBytes));
            _connectionInfo = new ConnectionInfo("sftp.vipps.no", "test", new PrivateKeyAuthenticationMethod("test", privateKey));
        }

        public async Task<List<string>> ListSubAccounts()
        {
            using (var sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();
                
                return sftp
                    .ListDirectory($"/settlements/inbox/xml/{_settings.OrgNo}")
                    .Where(f => f.IsDirectory)
                    .Select(f => f.Name)
                    .ToList();
            }
        }

        public async Task<List<string>> ListSettlements(string subAccount)
        {
            using (var sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();

                var files = await Task.Factory.FromAsync(sftp.BeginListDirectory($"/settlements/inbox/xml/{_settings.OrgNo}/{subAccount}", null, null), sftp.EndListDirectory);

                var settlements = files
                    .Where(f => f.IsRegularFile)
                    .Where(f => string.Equals(Path.GetExtension(f.Name), ".xml", StringComparison.OrdinalIgnoreCase))
                    .Select(f => Path.GetFileNameWithoutExtension(f.Name).Split("-")[1])
                    .ToList();
                return settlements;
            }
        }

        public async Task<Stream> GetSettlementFile(string subAccount, string settlement)
        {
            using (var sftp = new SftpClient(_connectionInfo))
            {
                sftp.Connect();
                var stream = new MemoryStream();
                await Task.Factory.FromAsync(sftp.BeginDownloadFile($"/settlements/inbox/xml/{_settings.OrgNo}/{subAccount}/{subAccount}-{settlement}.xml", stream), sftp.EndDownloadFile);

                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }
    }

    public class VippsSettings 
    {
        public string SftpPrivateKey { get; set; }
        public string OrgNo { get; set; }
    }
}
