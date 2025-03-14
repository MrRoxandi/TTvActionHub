﻿using TwitchLib.Client.Enums;

namespace TTvActionHub.LuaTools.Stuff
{
    public class Users
    {
        public enum USERLEVEL : int
        {
            VIEWIER, VIP, SUB, MODERATOR, BROADCASTER
        }

        public static USERLEVEL ParceFromTwitchLib(UserType type, bool issub, bool isvip)
        {
            return type switch
            {
                UserType.Moderator or UserType.GlobalModerator or UserType.Staff or UserType.Admin => USERLEVEL.MODERATOR,
                UserType.Broadcaster => USERLEVEL.BROADCASTER,
                _ => issub ? USERLEVEL.SUB : isvip ? USERLEVEL.VIP : USERLEVEL.VIEWIER,
            };
        }
    }
}
