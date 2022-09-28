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
        public bool IsLoginSupported => true;
        public bool AutoLogin => true;
        private string _loginData;

        //public void Login(Action<Result> callback = null)
        //{

        //}

        public void Logout(Action<Result> callback = null)
        {
            _localUser = null;
            _loginData = string.Empty;
        }

        public string GetUserInfo()
        {
            return IsLoggedIn ? _loginData : "{}";
        }

        /// <summary>
        /// 记录登录回调的事件
        /// </summary>
        private List<Action<Result>> _loginCallbacks = new List<Action<Result>>();

        public void Login(Action<Result> callback = null)
        {
            if (callback != null)
                _loginCallbacks.Add(callback);

            _firebaseAuth = FirebaseAuth.DefaultInstance;
            if (_localUser != null && _firebaseAuth.CurrentUser != null)
            {
                OnFirebaseAuthStateChange(null,null);
                return;
            }

            //因为play game 给集成到gamecenter 里面去了,所有这里需要接入gamecenter的gp登陆
            _managerInstance.LoginWith("GameCenter", delegate(Result result)
            {
                if (!result.Success)
                {
                    Logger.Debug($"fireabse login faill {result.Code} {result.Error}");
                    OnLoginFail(0, callback);
                    return;
                }

                //_localUser = _firebaseAuth.CurrentUser;
                //_loginData = result.LoginData.UserId;
                //OnLoginResult(result);

                ///拿到gp的token 然后去获取credential去登录firebase
                Credential credential = PlayGamesAuthProvider.GetCredential(result.LoginData.Token);
                Logger.Debug($"Login: GooglePlay Credential：{credential}");
                _firebaseAuth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        OnLoginFail(1);
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
        /// 响应登陆完成的回调事件
        /// </summary>
        /// <param name="result"></param>
        private void OnLoginResult(in Result result)
        {
            foreach (var callback in _loginCallbacks)
            {
                callback.Invoke(result);
            }
            _loginCallbacks.Clear();
        }

        //private void FillSuccessResult(ref Result result)
        //{
        //    result.Success = true;
        //    result.PluginName = Name;

            //result.UserInfo = new UserInfo
            //{
            //    UserId = _localUser.UserId,
            //    NickName = _localUser.DisplayName
            //};
        //}

        /// <summary>
        /// 登录成功
        /// </summary>
        /// <param name="result"></param>
        /// <param name="callback"></param>
        private void OnLoginSuccess(Result result, Action<Result> callback = null)
        {
            //FillSuccessResult(ref result);
            //OnLoginResult(result);
            Logger.Debug("OnLoginSuccess");
            callback?.Invoke(result);
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
                for (int i = 0;i < _loginCallbacks.Count; i++)
                {
                    _loginCallbacks[i]?.Invoke(result);
                }
                _loginCallbacks.Clear();
            }
        }

        /// <summary>
        /// 登录失败
        /// </summary>
        /// <param name="result"></param>
        private void OnLoginFailList(int errorCode = 0)
        {
            if (_loginCallbacks != null)
            {
                for (int i = 0; i < _loginCallbacks.Count; i++)
                {
                    _loginCallbacks[i]?.Invoke(new Result
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

            Logger.Error($"登录失败 code {errorCode} name {Name}");
        }

        /// <summary>
        /// firebase 切换账号了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="_"></param>
        void OnFirebaseAuthStateChange(object sender, System.EventArgs eventArgs)
        {
            _firebaseAuth = FirebaseAuth.DefaultInstance;
            var currentUser = _firebaseAuth.CurrentUser;
            //_localUser = currentUser;

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
                _loginData = _localUser.UserId;
                if (signedIn)
                {
                    Logger.Debug("Signed in " + _localUser.UserId);
                    //OnLoginResult(new Result
                    //{
                    //    Success = true,
                    //    PluginName = Name,
                    //});
                }

                GetToken(_localUser);
            }
            else
            {
                GetToken(_localUser);
            }
        }

        /// <summary>
        /// 获取token
        /// </summary>
        /// <param name="currentUser"></param>
        /// <param name="callback"></param>
        public void GetToken(FirebaseUser currentUser)
        {
            if (currentUser == null)
            {
                OnLoginFailList(5);
                return;
            }
            currentUser.TokenAsync(true).ContinueWithOnMainThread(task =>
            {
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
                OnLoginSuccessList(new Result
                {
                    Success = true,
                    PluginName = Name,
                    LoginData = new LoginResult
                    {
                        UserId = _localUser.UserId,
                        NickName = _localUser.DisplayName,
                        Token = idToken,
                    }
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