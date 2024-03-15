using System;


    public interface IDriver
    {
        public VehicleSP Vehicle { get; }
        public void TakeDamage(float _damage);
    }


