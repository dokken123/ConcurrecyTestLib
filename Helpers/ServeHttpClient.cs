using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Serve.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using Serve.Shared.JsonSerializer;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ConcurrencyTestLib.Helpers
{
    /// <summary>
    /// Contains http body along with returned HTTPS status code. 
    /// Useful for rest. For example, if one would like to retrieve 
    /// a user by email but user does not exist HTTP status should be
    /// 404 (resource not found) but not 200 (Success!). 
    /// </summary>
    public class ServeWebResponse
    {
        /// <summary>
        /// Gets or sets HttpStatusCode.
        /// </summary>
        /// <value>
        /// The http status code.
        /// </value>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>
        /// Gets or sets Request Data.
        /// </summary>
        /// <value>
        /// The request data.
        /// </value>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets Processing Time .
        /// </summary>
        /// <value>
        /// The request time taken for processing the request.
        /// </value>
        public TimeSpan ProcessingTime { get; set; }

    }
    /// <summary>
    /// Implements basic HTTP operations PUT and GET.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass",
        Justification = "Do you call ServeWebResponse a class, which deserves its own file? No way! ")]
    public class ServeHttpClient
    {
        bool UseCert = false;

        /// <summary>
        /// Instance of logWriter from Serve diagnostic framework.
        /// </summary>

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClient"/> class. 
        /// </summary>
        /// <param name="logWriter">
        /// The log Writer.
        /// </param>
        public ServeHttpClient(X509Certificate certificate = null)
            : this(certificate, false)
        {
        }

        public ServeHttpClient()
            : this( null, false)
        {
        }

        public ServeHttpClient(X509Certificate certificate, bool useCert)
        {
            this.Timeout = 60000; // 30 sec --
            this.SendChunked = false;
            this.MimeType = "application/json";
            this.CharSet = "charset=\"utf-8\"";
            this.UserAgent = "Serve Tess Harness Client";
            this.Certificate = certificate;
            this.UseCert = useCert;

            ServicePointManager.DefaultConnectionLimit = 256;

            if (certificate == null)
            {
                this.InitCertificate();
            }
        }

        private void InitCertificate()
        {
            string mname = "YinTongServiceClient::InitCertificate";

            StoreLocation certificateStore = StoreLocation.LocalMachine;
            bool enableCertificateAuthentication = true;
            string certificateName;

            try
            {
                // don't remove .ToString(), it insure that the varible exists -- 
                enableCertificateAuthentication = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableCertificateAuthentication"].ToString());
                if (!enableCertificateAuthentication) return;

                certificateStore = (StoreLocation)Enum.Parse(typeof(StoreLocation), ConfigurationManager.AppSettings["CertificateStore"]);
                certificateName = ConfigurationManager.AppSettings["CertificateName"].ToString();
            }
            catch (Exception ex)
            {
                throw new SettingsPropertyNotFoundException();
            }

            if (enableCertificateAuthentication)
            {
                X509Store store = new X509Store("MY", certificateStore);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindBySubjectName, certificateName, false);
                if (fcollection.Count == 0)
                {
                    string msg = string.Format("{0}: Requested certificate CN=[{1}] in Certificate Store [{2}] not found",
                        mname, certificateName, certificateStore);
                    throw new ApplicationException(msg);
                }
                this.Certificate = fcollection[0];
            }
        }

        // TODO: should properties below come from config section in config file? -- 
        /// <summary>
        /// Gets or Sets Client Certificate 
        /// </summary>
        public X509Certificate Certificate { get; set; }

        /// <summary>
        /// Gets or sets Timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public int Timeout { get; set; }

        // milliseconds 

        /// <summary>
        /// Gets or sets a value indicating whether SendChunked.
        /// </summary>
        /// <value>
        /// The send chunked.
        /// </value>
        public bool SendChunked { get; set; }

        /// <summary>
        /// Gets or sets MimeType.
        /// </summary>
        /// <value>
        /// The mime type.
        /// </value>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets CharSet.
        /// </summary>
        /// <value>
        /// The char set.
        /// </value>
        public string CharSet { get; set; }

        /// <summary>
        /// Gets or sets UserAgent.
        /// </summary>
        /// <value>
        /// The user agent.
        /// </value>
        public string UserAgent { get; set; }

        /// <summary>
        /// Basic HTTP POST, returns Response Data.
        /// </summary>
        /// <param name="uri">
        /// Url string.
        /// </param>
        /// <param name="data">
        /// Request Data to be submitted in HTTP body.
        /// </param>
        /// <returns>
        /// Http status code and http request body.
        /// </returns>
        public ServeWebResponse HttpPOST(string uri, string data, out TimeSpan durration)
        {
            Trace.Assert(!string.IsNullOrEmpty(uri));
            Trace.Assert(!string.IsNullOrEmpty(data));

            return this.GetHttpRequest(uri, data, "POST", out durration);
        }

        public ServeWebResponse HttpPOST(string uri, string data)
        {
            TimeSpan duration = TimeSpan.MinValue;
            return this.GetHttpRequest(uri, data, "POST", out duration);
        }

        /// <summary>
        /// Basic HTTP PUT, returns Response Data.
        /// </summary>
        /// <param name="uri">
        /// Url string.
        /// </param>
        /// <param name="data">
        /// Request Data.
        /// </param>
        /// <returns>
        /// Http status code and http request body.
        /// </returns>
        public ServeWebResponse HttpPUT(string uri, string data)
        {
            Trace.Assert(!string.IsNullOrEmpty(uri));
            Trace.Assert(!string.IsNullOrEmpty(data));

            return this.GetHttpRequest(uri, data, "PUT");
        }

        /// <summary>
        /// Wraps HTTP GET. 
        /// </summary>
        /// <param name="uri">
        /// Url string.
        /// </param>
        /// <returns>
        /// Http status code and http request body.
        /// </returns>
        public ServeWebResponse HttpGET(string uri)
        {
            Trace.Assert(!string.IsNullOrEmpty(uri));

            return this.GetHttpRequest(uri, null, "GET");
        }

        private ServeWebResponse GetHttpRequest(string uri, string data, string httpMethod)
        {
            TimeSpan duration = TimeSpan.MinValue;
            return this.GetHttpRequest(uri, data, httpMethod, out duration);
        }

        /// <summary>
        /// The is the main method for the Web Request. It does everything: GET, PUT and POST.
        /// </summary>
        /// <param name="uri">
        /// The uri string.
        /// </param>
        /// <param name="data">
        /// The request data.
        /// </param>
        /// <param name="httpMethod">
        /// The http method.
        /// </param>
        /// <returns>
        /// Http status code and http request body.
        /// </returns>
        private ServeWebResponse GetHttpRequest(string uri, string data, string httpMethod, out TimeSpan duration)
        {
            string mname = "HttpClient::GetHttpRequest()";
            string ServeCertName = string.IsNullOrEmpty(ConfigurationManager.AppSettings["ServeCertName"]) ? string.Empty : ConfigurationManager.AppSettings["ServeCertName"];
            string TestHarnessCertName = string.IsNullOrEmpty(ConfigurationManager.AppSettings["TestHarnessCertName"]) ? string.Empty : ConfigurationManager.AppSettings["TestHarnessCertName"];
            //bool UseCert = bool.Parse(ConfigurationManager.AppSettings["UseCert"].ToString());
            string authHeaderFormat = string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthHeaderFormat"]) ? string.Empty : ConfigurationManager.AppSettings["AuthHeaderFormat"];
            string authHeaderName = string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthHeaderName"]) ? string.Empty : ConfigurationManager.AppSettings["AuthHeaderName"];
            string sourceIdHeaderName = string.IsNullOrEmpty(ConfigurationManager.AppSettings["SourceIdHeaderName"]) ? string.Empty : ConfigurationManager.AppSettings["SourceIdHeaderName"];
            string sourceIdHeaderValue = string.IsNullOrEmpty(ConfigurationManager.AppSettings["SourceIdHeaderValue"]) ? string.Empty : ConfigurationManager.AppSettings["SourceIdHeaderValue"];
            string hashFormat = string.IsNullOrEmpty(ConfigurationManager.AppSettings["HashFormat"]) ? string.Empty : ConfigurationManager.AppSettings["HashFormat"];

            TimeZoneInfo TimeZone = null;

            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["TimeZone"]))
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(ConfigurationManager.AppSettings["TimeZone"]);
            }

            WebResponse response = null;
            string responseString = null;
            StringBuilder sb;
            HttpStatusCode httpStatusCode = HttpStatusCode.OK;
            TimeSpan getResponseTimeTaken;
            DateTime requestStartTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

            try
            {
                HttpWebRequest request;
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.UserAgent = this.UserAgent;
                request.Timeout = this.Timeout;
                request.Method = httpMethod;
                //request.KeepAlive = true;

                // Override Web Proxy here !! //

                data = data.Replace(Environment.NewLine, "");


                if (UseCert)
                {
                    string signature_method = string.IsNullOrEmpty(ConfigurationManager.AppSettings["SignatureMethod"]) ? string.Empty : ConfigurationManager.AppSettings["SignatureMethod"];
                    string algorithmName = string.IsNullOrEmpty(ConfigurationManager.AppSettings["AlgorithmName"]) ? string.Empty : ConfigurationManager.AppSettings["AlgorithmName"];

                    var certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    certStore.Open(OpenFlags.ReadOnly);
                    var cert = certStore.Certificates.Find(X509FindType.FindBySubjectName, TestHarnessCertName, false)[0];
                    certStore.Close();
                    //cert = new X509Certificate2(AppDomain.CurrentDomain.BaseDirectory + @"\ServeAPIDevCert.pfx", "Password1");

                    var nonce = Guid.NewGuid().ToString().Replace("-", "");
                    var timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                    var dataForHash = string.Format(hashFormat,
                        new object[] { request.Method, nonce, signature_method, timestamp, data });
                    var pvk = cert.PrivateKey as RSACryptoServiceProvider;
                    var sha1Sender = new SHA1Managed(); // new SHA256Managed();
                    var hashedSender = sha1Sender.ComputeHash(Encoding.UTF8.GetBytes(dataForHash));
                    var digSignSender = pvk.SignHash(hashedSender, CryptoConfig.MapNameToOID(algorithmName /*SHA256*/));
                    var digSignSenderBase64 = Convert.ToBase64String(digSignSender);
                    var authHeader = string.Format(authHeaderFormat,
                        new object[] { nonce, signature_method, digSignSenderBase64, timestamp });

                    request.Headers.Add(authHeaderName, authHeader);
                    request.Headers.Add(sourceIdHeaderName, sourceIdHeaderValue);
                }

                if ("POST" == httpMethod || "PUT" == httpMethod)
                {
                    request.ContentType = string.Format("{0};{1}", this.MimeType, this.CharSet);
                    //request.ContentLength = data.Length;

                    using (Stream stream = request.GetRequestStream())
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.Write(data.Normalize());
                            stream.Flush();
                            writer.Flush();
                            stream.Close();
                            writer.Close();
                        }
                    }
                }

                sb = new StringBuilder();
                for (var i = 0; i < request.Headers.Count; i++)
                {
                    sb.Append(request.Headers.Keys[i] + ": " + request.Headers[request.Headers.Keys[i]]);
                    sb.AppendLine();
                }
                sb.Append(data);

                requestStartTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone); 

                response = request.GetResponse();

                DateTime requestEndTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);

                getResponseTimeTaken = duration = requestEndTime - requestStartTime;
 
                Stream responseStream = response.GetResponseStream();
                if (responseStream != null)
                {
                    StreamReader reader = new StreamReader(responseStream);
                    responseString = reader.ReadToEnd();
                    responseStream.Close();
                }

                sb = new StringBuilder();
                for (var i = 0; i < response.Headers.Count; i++)
                {
                    sb.Append(response.Headers.Keys[i] + ": " + response.Headers[response.Headers.Keys[i]]);
                    sb.AppendLine();
                }
                sb.Append(responseString);
                if (UseCert)
                {
                    if (response.Headers[authHeaderName] != null)
                    {
                        Dictionary<string, string> authEntries = null;
                        string basicDigSignCompare = ParseHttp(response.Headers[authHeaderName], Encoding.UTF8.GetBytes(responseString), request.Method, out authEntries);

                        // load certificate
                        var certStoreHost = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                        certStoreHost.Open(OpenFlags.ReadOnly);
                        var certHost = certStoreHost.Certificates.Find(X509FindType.FindBySubjectName, ServeCertName, false)[0];
                        certStoreHost.Close();

                        // validate 
                        var hashedCampareConfirmed = ValidateDigitalSignature(certHost.PublicKey.Key, basicDigSignCompare, authEntries["signature"]);

                        if (!hashedCampareConfirmed)
                        {
                            throw new WebException("Verify Hash Failed");
                        }
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                // calculate duration //
                duration = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone) - requestStartTime;

                // Web Exception can come from two sources: WININET - timeout and HTTP errors such as 500 or 404 http statuses. 


                // an HTTP status/error in REST could be an important business information, for example, HTTP status
                // 404 actually can report that the data does not exist and it is a valid http request response 
                // For YinTong we do not seem have such challenge 

                // Try to retrieve more information about the error);
                if (wex.Response != null)
                {
                    HttpWebResponse errorResponse = (HttpWebResponse)wex.Response;

                }

                // re-throw the original exception with the stack, could be timeout or something else --   
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }

            if (!string.IsNullOrEmpty(responseString))
            {
                // format JSON nicely for easier reading -- 
                responseString = JsonHelper.Format(responseString);
            }

            // write formatted JSON response. 

            return new ServeWebResponse { HttpStatusCode = httpStatusCode, Data = responseString, ProcessingTime = getResponseTimeTaken};
        }


        private static string ParseHttp(string authorization, byte[] payload, string httpMethod, out Dictionary<string, string> authEntries)
        {
            string regexPattern = string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthHeaderRegexPattern"]) ? string.Empty : ConfigurationManager.AppSettings["AuthHeaderRegexPattern"];
            string hashFormat = string.IsNullOrEmpty(ConfigurationManager.AppSettings["HashFormat"]) ? string.Empty : ConfigurationManager.AppSettings["HashFormat"];
            var regEx = new Regex(regexPattern);
            authEntries = new Dictionary<string, string>();

            // get Authorization content //
            var matchResults = regEx.Match(authorization);

            while (matchResults.Success)
            {
                var value = matchResults.Value;
                if (value != "")
                {
                    value = value.Trim();
                    authEntries.Add(value.Substring(0, value.IndexOf('=')).ToLower(), value.Substring(value.IndexOf("=") + 1).Replace("\"", ""));
                }
                matchResults = matchResults.NextMatch();
            }

            var basicDigSignCompare = string.Format(hashFormat,
                new object[] { httpMethod, authEntries["nonce"], authEntries["signature-method"], authEntries["timestamp"], Encoding.UTF8.GetString(payload) });

            return basicDigSignCompare;
        }

        private static bool ValidateDigitalSignature(AsymmetricAlgorithm publicKey, string basicDigSignature, string digSignatureBase64)
        {
            var pubKey = publicKey as RSACryptoServiceProvider;
            var sha1Managed = new SHA1Managed();
            var hashedCampare = sha1Managed.ComputeHash(Encoding.UTF8.GetBytes(basicDigSignature));
            var hashedCampareConfirmed = pubKey.VerifyHash(hashedCampare, CryptoConfig.MapNameToOID("SHA1"), Convert.FromBase64String(digSignatureBase64));

            return hashedCampareConfirmed;
        }
    }
}
