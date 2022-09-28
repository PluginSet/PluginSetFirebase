#if ENABLE_FIREBASE_LOGIN
using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using PluginSet.Core;
using UnityEngine;

namespace PluginSet.Firebase
{
    public partial class PluginFirebase: ILoginPlugin
    {
        [Serializable]
        public class LoginData
        {
            [SerializeField]
            public string userId;
            [SerializeField]
            public string nickName;
            [SerializeField]
            public string token;
        }
        
        private FirebaseAuth _firebaseAuth;
        private FirebaseUser _localUser;

        public bool IsEnableLogin => true;
        public bool IsLoggedIn { get 
            {
#if UNITY_EDITOR
                return true;
#else
                return _loginData != null;
#endif
            } 
        }
        
        private string _loginData;

        public void Logout(Action<Result> callback = null)
        {
            _localUser = null;
            _loginData = string.Empty;
        }

        public string GetUserLoginData()
        {
            return IsLoggedIn ? _loginData : "{}";
        }

        /// <summary>
        /// 记录登录回调的事件
        /// </summary>
        private readonly List<Action<Result>> _loginCallbacks = new List<Action<Result>>();

        public void Login(Action<Result> callback = null)
        {
            if (callback != null)
                _loginCallbacks.Add(callback);

            _firebaseAuth = FirebaseAuth.DefaultInstance;
            
            Logger.Debug($"Login >>>>>>>>>>>>>>>>>>>>>>>  {_localUser}");
            if (_localUser != null && _firebaseAuth.CurrentUser != null)
            {
                GetToken(_localUser);
                return;
            }

            Logger.Debug($"Login Google ======================= ");
            //因为play game 给集成到gamecenter 里面去了,所有这里需要接入gamecenter的gp登陆
            _managerInstance.LoginWith("Google", delegate(Result result)
            {
                Logger.Debug($"Login Google result ======================= {result.Success}, {result.Code}, {result.Data}");
                if (!result.Success)
                {
                    Logger.Debug($"fireabse login faill {result.Code} {result.Error}");
                    OnLoginFail(PluginConstants.FailDefaultCode, callback);
                    return;
                }

                // 拿到gp的token 然后去获取credential去登录firebase
                var loginData = (PluginSet.Google.PluginGoogle.LoginData) result.DataObject;
                var credential = PlayGamesAuthProvider.GetCredential(loginData.code);
                Logger.Debug($"Login: GooglePlay Credential：{credential}");
                _firebaseAuth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    Logger.Debug($"Login: sign in {task.IsCanceled} {task.IsFaulted} {task.Exception}");
                    if (task.IsCanceled)
                    {
                        OnLoginFail(PluginConstants.CancelCode);
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        OnLoginFail(2);
                        return;
                    }

                    var user = task.Result;
                    Logger.Debug("Firebase sign in success with user: {0}: {1}", user.UserId, user.DisplayName);

                    _localUser = user;
                    _loginData = _localUser.UserId;
                    GetToken(_localUser);
                });
            });
        }

        /// <summary>
        /// 登录成功
        /// </summary>
        /// <param name="result"></param>
        private void OnLoginSuccessList(Result result)
        {
            Logger.Debug("OnLoginSuccessList");
            if (_loginCallbacks != null)
            {
                foreach (var t in _loginCallbacks)
                {
                    t?.Invoke(result);
                }

                _loginCallbacks.Clear();
            }
        }

        /// <summary>
        /// 登录失败
        /// </summary>
        /// <param name="errorCode"></param>
        private void OnLoginFailList(int errorCode = 0)
        {
            if (_loginCallbacks != null)
            {
                foreach (var t in _loginCallbacks)
                {
                    t?.Invoke(new Result
                    {
                        Success = false,
                        PluginName = Name,
                        Code = errorCode
                    });
                }

                _loginCallbacks.Clear();
            }
        }

        /// <summary>
        /// 登录失败
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="callback"></param>
        private void OnLoginFail(int errorCode = 0, Action<Result> callback = null)
        {
            callback?.Invoke(new Result
            {
                Success = false,
                PluginName = Name,
                Code = errorCode
            });

            Logger.Debug($"登录失败 code {errorCode} name {Name}");
        }

        /// <summary>
        /// firebase 切换账号了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        void OnFirebaseAuthStateChange(object sender, System.EventArgs eventArgs)
        {
            _firebaseAuth = FirebaseAuth.DefaultInstance;
            var currentUser = _firebaseAuth.CurrentUser;

            if (_localUser != null)
                Logger.Debug("OnFirebaseAuthStateChange ::: userId:{0}, userName:{1}", _localUser.UserId, _localUser.DisplayName);

            ShowNowFirebaseUserInfo();

            if (currentUser != _localUser) // signedIn
            {
                bool signedIn = _localUser != _firebaseAuth.CurrentUser && _firebaseAuth.CurrentUser != null;
                if (!signedIn && _localUser != null)
                {
                    Logger.Debug("Signed out " + _localUser.UserId);
                }
                _localUser = _firebaseAuth.CurrentUser;
                _loginData = _localUser?.UserId;
                
                if (signedIn)
                {
                    _loginData = null;
                    Logger.Debug("OnFirebaseAuthStateChange args: {0}", eventArgs.ToString());
                    SendNotification(PluginConstants.NOTIFY_LOGIN_STATUS_CHANGED, eventArgs.ToString());
                }
            }
        }

        /// <summary>
        /// 获取token
        /// </summary>
        /// <param name="currentUser"></param>
        public void GetToken(FirebaseUser currentUser)
        {
            Logger.Debug($"GetToken user >>> {currentUser}");
            if (currentUser == null)
            {
                OnLoginFailList(5);
                return;
            }
            
            currentUser.TokenAsync(true).ContinueWithOnMainThread(task =>
            {
                Logger.Debug($"GetToken user token task >>> {task.IsCanceled} {task.IsFaulted}, {task.Exception}");
                if (task.IsCanceled)
                {
                    OnLoginFailList(3);
                    return;
                }

                if (task.IsFaulted)
                {
                    OnLoginFailList(4);
                    return;
                }

                //_localUser = _firebaseAuth.CurrentUser;
                //_loginData = _localUser.UserId;
                string idToken = task.Result;
                Logger.Debug($"Login GetToken{idToken}");
                var loginData = new LoginData
                {
                    userId = _localUser.UserId,
                    nickName = _localUser.DisplayName,
                    token = idToken,
                };
                OnLoginSuccessList(new Result
                {
                    Success = true,
                    PluginName = Name,
                    Code = PluginConstants.SuccessCode,
                    Data = JsonUtility.ToJson(loginData),
                    DataObject = loginData,
                });
            });
        }

        private void ClearLoginState()
        {
            _firebaseAuth?.SignOut();
            _localUser = null;
        }

        private void InitFirebaseAuth()
        {
#if UNITY_EDITOR
            _firebaseAuth = FirebaseAuth.GetAuth(_appInstance);
#else
            _firebaseAuth = FirebaseAuth.DefaultInstance;
#endif
            _firebaseAuth.StateChanged += OnFirebaseAuthStateChange;
            OnFirebaseAuthStateChange(null, null);
        }

        /// <summary>
        /// 显示当前用户信息
        /// </summary>
        private void ShowNowFirebaseUserInfo()
        {
            if (_localUser != null)
            {
                Logger.Debug($"Login: Firebase Auth DisplayName _localUser:{_localUser.DisplayName}");
                Logger.Debug($"Login: Firebase Auth UserId _localUser:{_localUser.UserId}");
            }
            else if (_firebaseAuth.CurrentUser != null)
            {
                Logger.Debug($"Login: Firebase Auth DisplayName:{_firebaseAuth.CurrentUser.DisplayName}");
                Logger.Debug($"Login: Firebase Auth UserId:{_firebaseAuth.CurrentUser.UserId}");
            }
        }
    }
}
#endif