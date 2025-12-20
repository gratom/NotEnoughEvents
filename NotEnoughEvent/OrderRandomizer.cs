using System.Collections.Generic;
using System.Linq;
using SRF;
using UnityEngine;

namespace NEE
{
    public static class OrderRandomizer
    {
        public static Dictionary<PlaceTag, float> bacisWayPriceMultiplier = new Dictionary<PlaceTag, float>()
        {
            { PlaceTag.ground, 0.25f },
            { PlaceTag.water, 0.75f },
            { PlaceTag.air, 0.15f }
        };

        public static Order GenerateRandomOrder(PlaceTag placeTag, int routeIndex, int maxSum)
        {
            if (maxSum < 10)
            {
                return null;
            }

            List<City> cities = Consts.Cities.Where(x => x.routes.Any(r => r.placeTag == placeTag && r.routeIndex == routeIndex)).ToList();
            if (cities.Count <= 1)
            {
                return null;
            }

            City c1 = cities.Random();
            cities.Remove(c1);
            City c2 = cities.Random();

            Product p = Consts.products[c1.products.Random()];

            int sum = Random.Range(maxSum / 2, maxSum);

            int count = sum / p.priceBuy;
            if (count <= 0)
            {
                return null;
            }

            float range = Vector2.Distance(c1.pos, c2.pos);

            sum = p.priceBuy * count;
            Order order = new Order()
            {
                fromIndex = c1.id,
                toIndex = c2.id,
                productId = p.id,
                productCount = count,
                additionPrice = (int)(sum * bacisWayPriceMultiplier[placeTag]) + 1 + (int)((range + 100) * 0.01f)
            };
            return order;
        }
    }

    public enum PlaceTag
    {
        ground,
        water,
        air
    }

    public class Order
    {
        public int fromIndex;
        public int toIndex;

        public City from => fromIndex.ToCity();
        public City to => toIndex.ToCity();

        public int productId;
        public Product product => Consts.products[productId];

        public int productCount;
        public int additionPrice;

        public int totalBuyPrice => product.priceBuy * productCount;
        public int totalSellPrice => product.priceSell * productCount + additionPrice;
        public int totalProfit => totalSellPrice - totalBuyPrice;
    }

    public class City
    {
        public int id;
        public string name;
        public Vector2 pos;
        public Route[] routes;
        public int[] products;
    }

    public struct Product
    {
        public int id;
        public string name;
        public int priceBuy;
        public int priceSell;
    }

    public struct Route
    {
        public PlaceTag placeTag;
        public int routeIndex;

        public static bool operator ==(Route left, Route right)
        {
            return left.placeTag == right.placeTag
                   && left.routeIndex == right.routeIndex;
        }

        public static bool operator !=(Route left, Route right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Route other && this == other;
        }
    }

}