using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace LandIndexTool
{
    internal static class LandIndexLicense
    {
        private const string LicenseFileName = "DulangLicense.lic";
        private static readonly byte[] LicenseSecret =
            Convert.FromBase64String("RHVsYW5nTGFuZEluZGV4VG9vbC0yMDI2LUxpY2Vuc2UtS2V5LUIyN0Q5RjQz");

        public static LicenseStatus Validate()
        {
            var machineCode = GetMachineCode();
            foreach (var path in GetLicensePaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    var values = ReadLicense(path);
                    var status = ValidateValues(values, path, machineCode);
                    if (status.IsValid)
                    {
                        return status;
                    }

                    return status;
                }
                catch (Exception ex)
                {
                    return LicenseStatus.Invalid(path, machineCode, "授权文件读取失败：" + ex.Message);
                }
            }

            return LicenseStatus.Invalid("", machineCode, "未找到授权文件。");
        }

        public static string GetMachineCode()
        {
            var machineGuid = "";
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    machineGuid = key?.GetValue("MachineGuid") as string ?? "";
                }
            }
            catch
            {
                machineGuid = "";
            }

            var source = string.IsNullOrWhiteSpace(machineGuid)
                ? Environment.MachineName
                : machineGuid.Trim();
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("DULANG-LAND-INDEX|" + source));
                var hex = BitConverter.ToString(bytes).Replace("-", "").Substring(0, 20).ToUpperInvariant();
                return string.Join("-", Enumerable.Range(0, 5).Select(i => hex.Substring(i * 4, 4)));
            }
        }

        public static IEnumerable<string> GetLicensePaths()
        {
            var assemblyPath = "";
            try
            {
                assemblyPath = Assembly.GetExecutingAssembly().Location;
            }
            catch
            {
                assemblyPath = "";
            }

            if (!string.IsNullOrWhiteSpace(assemblyPath))
            {
                var folder = Path.GetDirectoryName(assemblyPath);
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    yield return Path.Combine(folder, LicenseFileName);
                }
            }

            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Autodesk",
                "ApplicationPlugins",
                "DulangLandIndexTool.bundle",
                "Contents",
                "Win64",
                LicenseFileName);

            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DulangLandIndexTool",
                LicenseFileName);

            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DulangLandIndexTool",
                LicenseFileName);
        }

        public static string BuildStatusMessage()
        {
            var status = Validate();
            var lines = new List<string>
            {
                "",
                "独狼建筑指标统计授权状态：",
                "机器码：" + status.MachineCode,
                "状态：" + (status.IsValid ? "已授权" : "未授权"),
                "说明：" + status.Message
            };

            if (!string.IsNullOrWhiteSpace(status.Customer))
            {
                lines.Add("客户：" + status.Customer);
            }

            if (!string.IsNullOrWhiteSpace(status.Expires))
            {
                lines.Add("到期：" + status.Expires);
            }

            lines.Add("授权文件查找位置：");
            foreach (var path in GetLicensePaths())
            {
                lines.Add(" - " + path);
            }

            return string.Join("\n", lines);
        }

        private static Dictionary<string, string> ReadLicense(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                result[key] = value;
            }

            return result;
        }

        private static LicenseStatus ValidateValues(Dictionary<string, string> values, string path, string machineCode)
        {
            var customer = GetValue(values, "Customer");
            var machine = NormalizeMachine(GetValue(values, "Machine"));
            var expires = GetValue(values, "Expires");
            var edition = GetValue(values, "Edition");
            var signature = GetValue(values, "Signature");

            if (string.IsNullOrWhiteSpace(customer) ||
                string.IsNullOrWhiteSpace(machine) ||
                string.IsNullOrWhiteSpace(expires) ||
                string.IsNullOrWhiteSpace(signature))
            {
                return LicenseStatus.Invalid(path, machineCode, "授权文件字段不完整。");
            }

            if (!string.Equals(signature, ComputeSignature(customer, machine, expires, edition), StringComparison.Ordinal))
            {
                return LicenseStatus.Invalid(path, machineCode, "授权签名无效。");
            }

            var normalizedCurrent = NormalizeMachine(machineCode);
            if (!string.Equals(machine, "ANY", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(machine, normalizedCurrent, StringComparison.OrdinalIgnoreCase))
            {
                return LicenseStatus.Invalid(path, machineCode, "授权文件不属于本机。");
            }

            if (!IsNeverExpires(expires))
            {
                if (!DateTime.TryParseExact(expires, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expireDate))
                {
                    return LicenseStatus.Invalid(path, machineCode, "授权到期日期格式错误，应为 yyyy-MM-dd 或 NEVER。");
                }

                if (DateTime.Today > expireDate)
                {
                    return LicenseStatus.Invalid(path, machineCode, "授权已到期。");
                }
            }

            return LicenseStatus.Valid(path, machineCode, customer, expires);
        }

        private static string ComputeSignature(string customer, string machine, string expires, string edition)
        {
            var text = CanonicalText(customer, machine, expires, edition);
            using (var hmac = new HMACSHA256(LicenseSecret))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)));
            }
        }

        private static string CanonicalText(string customer, string machine, string expires, string edition)
        {
            return "Customer=" + (customer ?? "").Trim() + "\n" +
                   "Machine=" + NormalizeMachine(machine) + "\n" +
                   "Expires=" + (expires ?? "").Trim().ToUpperInvariant() + "\n" +
                   "Edition=" + (edition ?? "Standard").Trim();
        }

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) ? value.Trim() : "";
        }

        private static string NormalizeMachine(string value)
        {
            value = (value ?? "").Trim().ToUpperInvariant();
            if (value == "ANY")
            {
                return value;
            }

            return value.Replace("-", "").Replace(" ", "");
        }

        private static bool IsNeverExpires(string expires)
        {
            return string.Equals(expires, "NEVER", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(expires, "PERPETUAL", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class LicenseStatus
    {
        public bool IsValid { get; private set; }
        public string Path { get; private set; }
        public string MachineCode { get; private set; }
        public string Customer { get; private set; }
        public string Expires { get; private set; }
        public string Message { get; private set; }

        public static LicenseStatus Valid(string path, string machineCode, string customer, string expires)
        {
            return new LicenseStatus
            {
                IsValid = true,
                Path = path,
                MachineCode = machineCode,
                Customer = customer,
                Expires = expires,
                Message = "授权有效。授权文件：" + path
            };
        }

        public static LicenseStatus Invalid(string path, string machineCode, string message)
        {
            return new LicenseStatus
            {
                IsValid = false,
                Path = path,
                MachineCode = machineCode,
                Customer = "",
                Expires = "",
                Message = message
            };
        }
    }
}
