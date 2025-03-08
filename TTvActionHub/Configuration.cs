﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLua;
using TTvActionHub.Items;
using TTvActionHub.Logs;
using TTvActionHub.Security;
using TTvActionHub.Twitch;
using TTvActionHub.LuaTools.Stuff;

namespace TTvActionHub
{
    public class Configuration : IConfig
    {
        private static string ClientId { get => "--"; }
        private static string ClientSecret { get => "--"; }
        private static string RedirectUrl { get => @"http://localhost:3000/"; }

        public Dictionary<string, Command> Commands { get => _commands; }
        public Dictionary<string, Reward> Rewards { get => _rewards; }
        public List<TActions> TActions { get => _tActions; }

        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo { get => _ttvInfo; }
        public bool LogState { get => _logsState; }
        public (string obr, string cbr) Brackets { get => new(_obracket, _cbracket); }

        private readonly (string Login, string ID, string Token, string RefreshToken) _ttvInfo;
        private readonly TwitchApi TwitchApi;

        private readonly long _stdCooldown;
        private readonly bool _logsState;

        private readonly Dictionary<string, Command> _commands;
        private readonly Dictionary<string, Reward> _rewards;
        private readonly List<TActions> _tActions;

        private readonly string _obracket;
        private readonly string _cbracket;

        private readonly string _configPath;

        private string FieldAdress(string field) => $"In [{_configPath}] field [{field}]";
        private string ParamAdress(string field, string param) => $"In [{_configPath}] parameter [{param}] in field [{field}]";
        
        public Configuration(string path)
        {

            var lua = new Lua();
            lua.State.Encoding = Encoding.UTF8;
            lua.LoadCLRPackage();

            var state = lua.DoFile(path);
            if (state[0] is not LuaTable luaConfig)
            {
                throw new Exception($"Failed to load controller configuration. Check the [{path}] config");
            }

            TwitchApi = new TwitchApi(ClientId, ClientSecret, RedirectUrl);

            if (luaConfig["force-relog"] is not bool isForceRelog)
            {
                Logger.Warn($"{FieldAdress("force-relog")} is not presented. Will be used default value: [{false}].");
                isForceRelog = false;
            } 
            _configPath = path;

            (string? Login, string? ID, string? Token, string? RefreshToken)? authInfo = null;

            if (!isForceRelog)
            {
                authInfo = AuthorizationManager.LoadInfo(ClientSecret);
                if (authInfo is (string, string, string, string) info)
                {
                    if (!TwitchApi.ValidateTokenAsync(info.Token).Result)
                    {
                        Logger.Info("Trying to update token");
                        var (AccessToken, RefreshToken) = TwitchApi.RefreshAccessTokenAsync(info.RefreshToken).Result;
                        info.Token = AccessToken;
                        info.RefreshToken = RefreshToken ?? authInfo?.RefreshToken;
                    }

                    authInfo = info;
                }
            }

            if(isForceRelog || authInfo == null)
            {
                var auth = TwitchApi.GetAuthorizationInfo().Result;
                if (auth.Login == null || auth.ID == null || auth.Token == null)
                    throw new Exception("Unable to get Authorization information");
                authInfo = auth;
            }

            _ttvInfo = new() { 
                Login = authInfo?.Login ?? "", 
                ID = authInfo?.ID ?? "", 
                Token = authInfo?.Token ?? "", 
                RefreshToken = authInfo?.RefreshToken ?? "" 
            };

            

            AuthorizationManager.SaveInfo(ClientSecret, _ttvInfo.Login, _ttvInfo.ID, _ttvInfo.Token, _ttvInfo.RefreshToken);
            
            if (luaConfig["opening-bracket"] is not string obracket || luaConfig["closing-bracket"] is not string cbracket)
            {
                //TODO: FIX
                //ConsoleLogger.Warn($"{FieldAdress("[opening-bracket] or [closing-bracket]")} is not presented. Ignoring...");
                obracket = String.Empty;
                cbracket = String.Empty;
            }

            _obracket = obracket;
            _cbracket = cbracket;

            if (luaConfig["logs"] is not bool logState)
            {
                Logger.Warn($"{FieldAdress("logs")} is not presented. Will be used default value: [{true}].");
                logState = true;
            }
            
            _logsState = logState;
            
            if (luaConfig["timeout"] is not long timeOut)
            {
                timeOut = 30 * 1000; // 30 seconds in milliseconds
                Logger.Warn($"{FieldAdress("timeout")} is not presented. Will be used default value: {timeOut}");
            }
            
            _stdCooldown = timeOut;

            _commands = [];
            _rewards = [];
            _tActions = [];

            LoadCommands(luaConfig["commands"] as LuaTable);
            LoadRewards(luaConfig["rewards"] as LuaTable);
            LoadTActions(luaConfig["tactions"] as LuaTable);

            ShowConfigInfo();

            Logger.Info($"Configuration loaded successfully");
        }

        private void LoadCommands(LuaTable? cmds) {
            
            if (cmds is null) {
                Logger.Warn($"{FieldAdress("commands")} is not presented. Ignoring...");
                return;
            }

            foreach (var keyObj in cmds.Keys)
            {

                if (cmds[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress("commands", $"{keyObj}")} is not a command. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress($"{keyObj}", "action")} is not a action. Check syntax.");

                if (table["timeout"] is not long timer)
                {
                    Logger.Warn($"{ParamAdress($"{keyObj}", "timeout")} is not presented. Will be used default value: {_stdCooldown} ms");
                    timer = _stdCooldown;
                }
                if (table["perm"] is not USERLEVEL perm)
                {
                    Logger.Warn($"{ParamAdress($"{keyObj}", "perm")} is not presented. Will be used default value: VIEWIER");
                    perm = USERLEVEL.VIEWIER;
                }
                            
                _commands.Add(keyObj.ToString()!, new Command { Function = action, Perm = perm, TimeOut = timer});
                Logger.Info($"Loaded comand: {keyObj}");
            }
        }

        private void LoadRewards(LuaTable? rewards)
        {
            if (rewards is null)
            {
                Logger.Warn($"{FieldAdress("rewards")} is not presented. Ignoring...");
                return;
            }
            
            foreach (var keyObj in rewards.Keys)
            {
                if (rewards[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress("rewards", $"{keyObj}")} is not a reward. Check syntax.");

                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress($"{keyObj}", "action")} is not a action. Check syntax.");
                
                _rewards.Add(keyObj.ToString()!, new Reward { Function = action });

                Logger.Info($"Loaded reward: {keyObj}");
            }
        }

        private void LoadTActions(LuaTable? events)
        {
            if (events is null)
            {
                Logger.Warn($"{FieldAdress("tactions")} is not presented. Ignoring...");
                return;
            }

            foreach (var keyObj in events.Keys)
            {
                if(events[keyObj] is not LuaTable table)
                    throw new Exception($"{ParamAdress("tactions", $"{keyObj}")} is not a event. Check syntax.");
                if (table["action"] is not LuaFunction action)
                    throw new Exception($"{ParamAdress($"{keyObj}", "action")} is not a action. Check syntax.");
                if (table["timeout"] is not long timeout)
                    throw new Exception($"{ParamAdress($"{keyObj}", "timeout")} is not a integer. Check syntax.");
                else if(timeout <= 0)
                    throw new Exception($"{ParamAdress($"{keyObj}", "timeout")} is not valid time. Allowed values (>= 1).");

                _tActions.Add(new TActions() { Action = action, Name = keyObj.ToString()!, TimeOut = timeout });

                Logger.Info($"Loaded TEvent: {keyObj}");
            }
        }

        private void ShowConfigInfo()
        {
            Logger.Info($"Login: {_ttvInfo.Login}");
            Logger.Info($"ID: {_ttvInfo.ID}");
            Logger.Info($"Token: found");
            Logger.Info($"Standart cooldown: {_stdCooldown}");
            Logger.Info($"Services logs state: {_logsState}");
            if (!string.IsNullOrEmpty(_obracket) && !string.IsNullOrEmpty(_cbracket))
                Logger.Info($"Brackets: {_obracket} and {_cbracket}");

        }

    }
}
