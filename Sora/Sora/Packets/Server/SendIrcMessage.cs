﻿#region LICENSE

/*
    Sora - A Modular Bancho written in C#
    Copyright (C) 2019 Robin A. P.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Sora.Enums;
using Sora.Helpers;
using Sora.Interfaces;

namespace Sora.Packets.Server
{
    public struct MessageStruct
    {
        public string Username;
        public string Message;
        public string ChannelTarget;
        public int SenderId;
    }

    public class SendIrcMessage : IPacket
    {
        public MessageStruct Msg;

        public SendIrcMessage(MessageStruct message) => Msg = message;

        public PacketId Id => PacketId.ServerSendMessage;

        public void ReadFromStream(MStreamReader sr)
        {
            Msg = new MessageStruct
            {
                Username = sr.ReadString(),
                Message = sr.ReadString(),
                ChannelTarget = sr.ReadString(),
                SenderId = sr.ReadInt32()
            };
        }

        public void WriteToStream(MStreamWriter sw)
        {
            sw.Write(Msg.Username);
            sw.Write(Msg.Message);
            sw.Write(Msg.ChannelTarget);
            sw.Write(Msg.SenderId);
        }
    }
}
