using System;
using System.Configuration;
namespace JHSoft.HalfRoad.Arch.Authentication.OAuth
{
	internal static class AuthenticationProviderSettings
	{
        private static readonly QConnectSDK.Config.QQConnectConfig m_qqCon = new QConnectSDK.Config.QQConnectConfig(); 
		private const string RedirectUriKey = "CallbackUri";
        public static readonly string AuthorizationEndpoint = m_qqCon.GetAuthorizeURL();
		public static readonly string TokenEndpoint = "https://graph.qq.com/oauth2.0/token";
		public static readonly string OpenIdEndpoint = "https://graph.qq.com/oauth2.0/me?{0}={1}";
		public static readonly string UserProfileEndpoint = "https://graph.qq.com/user/get_user_info?{0}={1}&{2}={3}&{4}={5}&{6}={7}";
        public static readonly Uri RedirectUri = m_qqCon.GetCallBackURI();
	}
}
