using UnityEngine;

namespace Klyte.WriteTheSigns.Rendering
{
    public class ParkItemCache : IItemCache
    {
        public byte parkId;
        public long? Id { get => parkId; set => parkId = (byte)(value ?? 0); }

        public FormatableString Name
        {
            get
            {
                if (name is null)
                {
                    name = new FormatableString(DistrictManager.instance.GetParkName(parkId));
                }
                return name;
            }
        }
        public int AreaSqMeters
        {
            get
            {
                if (areaSqMeters is null)
                {
                    if (parkId == 0)
                    {
                        areaSqMeters = 0;
                    }
                    else
                    {
                        int sizeSqSubcell = 0;
                        var inst = DistrictManager.instance;
                        for (int i = 0; i < 512; i++)
                        {
                            for (int j = 0; j < 512; j++)
                            {
                                int num = i * 512 + j;
                                DistrictManager.Cell cell = inst.m_parkGrid[num];
                                if (cell.m_alpha1 != 0 && cell.m_district1 == parkId)
                                {
                                    sizeSqSubcell++;
                                }
                                if (cell.m_alpha2 != 0 && cell.m_district2 == parkId)
                                {
                                    sizeSqSubcell++;
                                }
                                if (cell.m_alpha3 != 0 && cell.m_district3 == parkId)
                                {
                                    sizeSqSubcell++;
                                }
                                if (cell.m_alpha4 != 0 && cell.m_district4 == parkId)
                                {
                                    sizeSqSubcell++;
                                }
                            }
                        }
                        areaSqMeters = Mathf.RoundToInt(284.765625f * sizeSqSubcell);
                    }
                }
                return areaSqMeters ?? 0;
            }
        }

        private FormatableString name;
        private int? areaSqMeters;
        public void PurgeCache(CacheErasingFlags cacheToPurge, InstanceID refID)
        {
            if (cacheToPurge.Has(CacheErasingFlags.ParkName))
            {
                name = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.ParkArea))
            {
                areaSqMeters = null;
            }
        }
    }

}
