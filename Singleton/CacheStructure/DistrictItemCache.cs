using UnityEngine;

namespace Klyte.WriteTheSigns.Rendering
{
    public class DistrictItemCache : IItemCache
    {
        public byte districtId;
        public long? Id { get => districtId; set => districtId = (byte)(value ?? 0); }
        public FormatableString Name
        {
            get
            {
                if (name is null)
                {
                    name = new FormatableString(districtId == 0 ? SimulationManager.instance.m_metaData.m_CityName : DistrictManager.instance.GetDistrictName(districtId));
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
                    if (districtId == 0)
                    {
                        areaSqMeters = 1920 * 1920 * 81;
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
                                DistrictManager.Cell cell = inst.m_districtGrid[num];
                                if (cell.m_alpha1 != 0 && cell.m_district1 == districtId)
                                {
                                    sizeSqSubcell++;
                                }
                                if (cell.m_alpha2 != 0 && cell.m_district2 == districtId)
                                {
                                    sizeSqSubcell++;
                                }
                                if (cell.m_alpha3 != 0 && cell.m_district3 == districtId)
                                {
                                    sizeSqSubcell++;
                                }
                                if (cell.m_alpha4 != 0 && cell.m_district4 == districtId)
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
            if (cacheToPurge.Has(CacheErasingFlags.DistrictName))
            {
                name = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.DistrictArea))
            {
                areaSqMeters = null;
            }
        }
    }

}
