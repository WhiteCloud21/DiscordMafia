using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMafia.Roles.Places
{
    public class Place
    {
        private static Place[] _places;
        public string Name { get; protected set; }

        protected Place() { }

        public static Place[] AvailablePlaces
        {
            get
            {
                if (_places == null)
                {
                    _places = new Place[]
                    {
                        new Place() { Name = "Дом" },
                        new Place() { Name = "Супермаркет" },
                        new Place() { Name = "Стадион" },
                    };
                }
                return _places;
            }
        }
    }
}
