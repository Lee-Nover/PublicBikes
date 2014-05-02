using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bicikelj.Model
{
    public class CustomKeyGroup<T> : List<T>
    {
        public static List<Group<T>> GetItemGroups(IEnumerable<T> itemList, Func<T, string> getKeyFunc)
        {
            IEnumerable<Group<T>> groupList = from item in itemList
                                              group item by getKeyFunc(item) into g
                                              orderby g.Key
                                              select new Group<T>(g.Key, g);
            return groupList.ToList();
        }

        public class Group<Tg> : List<Tg>
        {
            public Group(string name, IEnumerable<Tg> items) : base(items)
            {
                this.Title = name;
            }

            public string Title { get; set; }
        }
    }
}
