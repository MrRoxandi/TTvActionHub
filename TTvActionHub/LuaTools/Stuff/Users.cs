using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Enums;

namespace TTvActionHub.LuaTools.Stuff
{
    public enum USERLEVEL: int
    {
        VIEWIER, VIP, SUB, MODERATOR, BROADCASTER
    }

    public class Users
    {
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
