using JHSoft.HalfRoad.Arch.Authentication.OAuth;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
namespace DotNetOpenAuth.AspNet.Clients
{
	public sealed class TencentOAuthClient : OAuth2Client
	{
		private static string AuthorizationEndpoint = AuthenticationProviderSettings.AuthorizationEndpoint;
		private static string TokenEndpoint = AuthenticationProviderSettings.TokenEndpoint;
		private readonly string AppId;
		private readonly string AppSecret;
		private string AccessToken
		{
			get;
			set;
		}
		public TencentOAuthClient(string appId, string appSecret, string providerName) : base(providerName)
		{
			this.AppId = appId;
			this.AppSecret = appSecret;
		}
		protected override Uri GetServiceLoginUrl(Uri returnUrl)
		{
			UriBuilder builder = new UriBuilder(TencentOAuthClient.AuthorizationEndpoint);
			builder.Query = returnUrl.Query.Substring(1).Replace("__sid__", "state");
			builder.AppendQueryArgs(new Dictionary<string, string>
			{

				{
					"response_type",
					"code"
				},

				{
					"client_id",
					this.AppId
				},

				{
					"redirect_uri",
					AuthenticationProviderSettings.RedirectUri.ToString()
				},

				{
					"scope",
					"get_user_info"
				}
			});
			return builder.Uri;
		}
		protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
		{
			UriBuilder builder = new UriBuilder(TencentOAuthClient.TokenEndpoint);
			builder.AppendQueryArgs(new Dictionary<string, string>
			{

				{
					"grant_type",
					"authorization_code"
				},

				{
					"client_id",
					this.AppId
				},

				{
					"client_secret",
					this.AppSecret
				},

				{
					"code",
					authorizationCode
				},

				{
					"state",
					Guid.NewGuid().ToString("N")
				},

				{
					"redirect_uri",
					AuthenticationProviderSettings.RedirectUri.ToString()
				}
			});
			string result;
			using (WebClient client = new WebClient())
			{
				string data = client.DownloadString(builder.Uri);
				if (string.IsNullOrEmpty(data))
				{
					result = null;
				}
				else
				{
					NameValueCollection parsedQueryString = HttpUtility.ParseQueryString(data);
					result = parsedQueryString["access_token"];
				}
			}
			return result;
		}
		public override AuthenticationResult VerifyAuthentication(HttpContextBase context, Uri returnPageUrl)
		{
			string userName = null;
			string authorizationCode = context.Request.QueryString["code"];
			AuthenticationResult result;
			if (string.IsNullOrEmpty(authorizationCode))
			{
				result = AuthenticationResult.Failed;
			}
			else
			{
				string accessToken = this.QueryAccessToken(returnPageUrl, authorizationCode);
				if (accessToken == null)
				{
					result = AuthenticationResult.Failed;
				}
				else
				{
					this.AccessToken = accessToken;
					string openId = this.QueryOpenId(returnPageUrl, accessToken);
					if (openId == null)
					{
						result = AuthenticationResult.Failed;
					}
					else
					{
						IDictionary<string, string> userData = this.GetUserData(openId);
						if (userData == null)
						{
							result = AuthenticationResult.Failed;
						}
						else
						{
							string providerUserId = openId;
							if (!userData.TryGetValue("nickname", out userName))
							{
								userName = providerUserId;
							}
							userData["access_token"] = authorizationCode;
							result = new AuthenticationResult(true, base.ProviderName, providerUserId, userName, userData);
						}
					}
				}
			}
			return result;
		}
		private string QueryOpenId(Uri returnUrl, string accessToken)
		{
			string url = string.Format(AuthenticationProviderSettings.OpenIdEndpoint, "access_token", accessToken);
			WebRequest request = WebRequest.Create(url);
			string result;
			using (WebResponse response = request.GetResponse())
			{
				using (Stream responseStream = response.GetResponseStream())
				{
					using (StreamReader reader = new StreamReader(responseStream))
					{
						string json = string.Empty;
						while (!reader.EndOfStream)
						{
							json += reader.ReadLine();
						}
						json = json.Substring(json.IndexOf('{'), json.LastIndexOf(')') - json.IndexOf('{'));
						JavaScriptSerializer serializer = new JavaScriptSerializer();
						Dictionary<string, object> dictionary = (Dictionary<string, object>)serializer.DeserializeObject(json);
						string openId = dictionary["openid"].ToString();
						result = openId;
					}
				}
			}
			return result;
		}
		protected override IDictionary<string, string> GetUserData(string openId)
		{
			string userProfileEndpoint = string.Format(AuthenticationProviderSettings.UserProfileEndpoint, new object[]
			{
				"access_token",
				this.AccessToken,
				"oauth_consumer_key",
				this.AppId,
				"openid",
				openId,
				"format",
				"json"
			});
			WebRequest request = WebRequest.Create(userProfileEndpoint);
			IDictionary<string, string> result;
			using (WebResponse response = request.GetResponse())
			{
				using (Stream responseStream = response.GetResponseStream())
				{
					using (StreamReader reader = new StreamReader(responseStream))
					{
						string json = string.Empty;
						while (!reader.EndOfStream)
						{
							json += reader.ReadLine();
						}
						json = json.Substring(json.IndexOf('{'));
						JavaScriptSerializer serializer = new JavaScriptSerializer();
						Dictionary<string, object> dictionary = (Dictionary<string, object>)serializer.DeserializeObject(json);
						Dictionary<string, string> userData = new Dictionary<string, string>();
						foreach (KeyValuePair<string, object> item in dictionary)
						{
							userData.Add(item.Key, item.Value.ToString());
						}
						result = userData;
					}
				}
			}
			return result;
		}
	}
}
