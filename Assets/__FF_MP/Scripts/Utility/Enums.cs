public static class Enums
{
    public enum GraphicsMode { Low, High }
    public enum Platform { Desktop, Mobile }
    public enum AnimType { Scale, Position }


    // Inputs
    public enum InputType { Arrows, Joystick }
    public enum SwipeDirection { None, Up, Down, Left, Right };
    public enum Direction { None, Forward, Backward, Left, Right };


    // Rider
    public enum VehicleType { TwoWheeler, FourWheeler }
    public enum UpgradeType { Speed, Engine, Mileage }


    // Maps
    public enum OrderType { InStock, ToCollect };
    public enum OrderCollectingBy { None, Player, Rival };
    public enum BoosterType { Speed, Health, Fuel, Acceleration, None }
    public enum InitialBoosterType { Speed, Engine, NoBot, None }
    public enum ObstacleType { Still, Dynamic }
    public enum ObstacleVarient { Tree, JCB, SchoolBus, CourierVan, WoodTruck, OilTanker, FireBrigade }


    // Dev
    public enum ReleaseMode { Debug, Release };


    // Level
    public enum Objective { DeliverOrders, CollectPoints }
    public enum ObjectiveCondition { None, MinHealth, InTime, Speed }
    public enum CostType { POINTS, ORARE }


    // Dish
    public enum DishRegion
    {
        Any, Caribbean, Australia_and_New_Zealand,
        Southern_Asia, Central_Asia, Northern_Asia, Western_Asia, East_Asia, Eastern_Asia, South___Eastern_Asia,
        Southern_Europe, Northern_Europe, Western_Europe, Eastern_Europe,
        Central_America, South_America, Northern_America,
        Middle_Africa, Eastern_Africa, Northern_Africa, Southern_Africa, Western_Africa
    }
    public enum DishContinent { Any, North_America, Europe, Asia, Africa, South_America, Australia, Oceania }
    public enum DishCountry { Any, India, Jamaica, Philippines, Poland, Australia }
    public enum DishDiet { Any, NON_VEGETARIAN, VEGAN, VEGETARIAN }
    public enum DishMethod { Any, Raw, Boil, Cook, Grill, Fry, Bake, Roast, Toast, Steam, Fried, Baked }
    public enum DishBrand { Any, OneRare, Chef_Special, Cornitos, The_Bhukkad_Cafe, Glocal_Junction, Art_of_Dum, Indian_Bistro, China_Bistro, Papa_Johns, Masterchow, Get_Fudo, MAGGI, Miam, Bagrry__s, Wingreens_Farms, To_Be_Honest, Burgrill, Salad_Days, }
    public enum DishMeter { Any, Skill_Meter, Time_Meter, Spice_Meter }
    public enum MeterCondition { None, Equals_To, Less_Than, Greater_Than }
}
