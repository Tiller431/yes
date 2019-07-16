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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using JetBrains.Annotations;

namespace Sora.Database.Models
{
    [UsedImplicitly]
    public class Friends
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [UsedImplicitly]
        public int FriendId { get; set; }

        public static IEnumerable<int> GetFriends(SoraDbContextFactory factory, int userId)
        {
            return factory.Get()
                          .Friends
                          .Where(t => t.UserId == userId)
                          .Select(x => x.FriendId).ToList();
        }

        public static void AddFriend(SoraDbContextFactory factory, int userId, int friendId)
        {
            using var db = factory.GetForWrite();

            db.Context.Friends.Add(new Friends {UserId = userId, FriendId = friendId});
        }

        public static void RemoveFriend(SoraDbContextFactory factory, int userId, int friendId)
        {
            using var db = factory.GetForWrite();

            db.Context.RemoveRange(db.Context.Friends.Where(x => x.UserId == userId && x.FriendId == friendId));
        }
    }
}
