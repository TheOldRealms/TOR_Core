using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.Extensions
{
    public static class CollectionExtensions
    {
        public static void AddIfNotNull<T>(this List<T> list, T item)
        {
            if (item != null)
            {
                list.Add(item);
            }
        }

        public static void RemoveIfExists<T>(this IEnumerable<T> list, T item) where T : class
        {
            list = list.Where(x => x != item);
        }

        public static void RemoveAllOfType<T>(this IEnumerable<T> list, Type type) where T : class
        {
            list = list.Where(x => x.GetType() != type);
        }

        public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> collection, int take)
        {
            var random = new Random();
            var available = collection.Count();
            var needed = take;
            foreach (var item in collection)
            {
                if (random.Next(available) < needed)
                {
                    needed--;
                    yield return item;
                    if (needed == 0)
                    {
                        break;
                    }
                }
                available--;
            }
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        public static void AddEquipment(this MBEquipmentRoster roster, Equipment equipment)
        {
            MBList<Equipment> equipments = (MBList<Equipment>)AccessTools.Field(typeof(MBEquipmentRoster), "_equipments").GetValue(roster);
            equipments.Add(equipment);
        }
    }
}
