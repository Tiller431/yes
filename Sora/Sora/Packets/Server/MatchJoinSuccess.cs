#region LICENSE

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

using System;
using Sora.Enums;
using Sora.Helpers;
using Sora.Interfaces;
using Sora.Objects;

namespace Sora.Packets.Server
{
    public class MatchJoinSuccess : IPacket
    {
        public MultiplayerRoom Room;

        public MatchJoinSuccess(MultiplayerRoom room) => Room = room;

        public PacketId Id => PacketId.ServerMatchJoinSuccess;

        public void ReadFromStream(MStreamReader sr)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(MStreamWriter sw)
        {
            sw.Write(Room);
        }
    }
}
