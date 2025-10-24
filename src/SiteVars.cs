//  Authors:  Brian Miranda, Robert M. Scheller

using Landis.SpatialModeling;
using Landis.Library.UniversalCohorts;
using System.Dynamic;
using System.Threading;

namespace Landis.Extension.Browse
{
    public static class SiteVars
    {
        private static ISiteVar<double> sitePreference;
        private static ISiteVar<SiteCohorts> cohorts;
        private static ISiteVar<double> neighborhoodForage;
        private static ISiteVar<double> browseIndex;
        private static ISiteVar<double> browseDisturbance;
        private static ISiteVar<int> populationZone;
        private static ISiteVar<double> forageQuantity;
        private static ISiteVar<double> habitatSuitability;
        private static ISiteVar<double> localPopulation;
        private static ISiteVar<double> siteCapacity;
        private static ISiteVar<double> weightedBrowse;
        private static ISiteVar<double> cappedBrowse;  
        private static ISiteVar<double> remainBrowse;
        private static ISiteVar<double> totalBrowse;
        private static ISiteVar<double> biomassRemoved;
        private static ISiteVar<int> cohortsDamaged;
        private static ISiteVar<ExpandoObject> additionalFields;
        private static ExpandoObject fieldsToAdd;

        //private static ISiteVar<int> ecoMaxBiomass;
        //private static ISiteVar<List<Landis.Library.BiomassCohorts.ICohort>> siteCohortList;

        //public static ISiteVar<Dictionary<int, Dictionary<int, double>>> Forage;
        //public static ISiteVar<Dictionary<int, Dictionary<int, double>>> ForageInReach;
        //public static ISiteVar<Dictionary<int, Dictionary<int, double>>> LastBrowseProportion;


        //private static ISiteVar<int> youngCohortCount;

        //---------------------------------------------------------------------

        public static void Initialize()
        {
            sitePreference = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            neighborhoodForage = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            browseIndex = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            browseDisturbance = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            populationZone = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            forageQuantity = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            habitatSuitability = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            localPopulation = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            siteCapacity = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            weightedBrowse = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            cappedBrowse = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            remainBrowse = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            totalBrowse = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            biomassRemoved = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            cohortsDamaged = PlugIn.ModelCore.Landscape.NewSiteVar<int>();

            additionalFields = PlugIn.ModelCore.Landscape.NewSiteVar<ExpandoObject>();

            //foreach(ActiveSite site in PlugIn.ModelCore.Landscape.ActiveSites)
            //{
            //    //Forage[site] = new Dictionary<int, Dictionary<int, double>>();
            //    //ForageInReach[site] = new Dictionary<int, Dictionary<int, double>>();
            //    //LastBrowseProportion[site] = new Dictionary<int, Dictionary<int, double>>();
            //}

            cohorts = PlugIn.ModelCore.GetSiteVar<Landis.Library.UniversalCohorts.SiteCohorts>("Succession.UniversalCohorts");
            RegisterAdditionalFields();

        }
        public static void RegisterAdditionalFields()
        {
            foreach (ActiveSite site in PlugIn.ModelCore.Landscape)
            {
                SiteVars.AdditionalFields[site] = fieldsToAdd;
            }
            PlugIn.ModelCore.RegisterSiteVar(additionalFields, "Other.AdditionalFields");
        }

        public static void SetAdditionalFields(ExpandoObject addFields)
        {
            fieldsToAdd = addFields;
        }


        //---------------------------------------------------------------------
        public static ISiteVar<SiteCohorts> Cohorts
        {
            get
            {
                return cohorts;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<ExpandoObject> AdditionalFields
        {
            get
            {
                return additionalFields;
            }
            set
            {
                additionalFields = value;
            }
        }
        //---------------------------------------------------------------------
        //---------------------------------------------------------------------
        public static ISiteVar<double> SitePreference
        {
            get {
                return sitePreference;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> NeighborhoodForage
        {
            get
            {
                return neighborhoodForage;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> BrowseIndex
        {
            get
            {
                return browseIndex;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<int> PopulationZone
        {
            get
            {
                return populationZone;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> ForageQuantity
        {
            get
            {
                return forageQuantity;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> HabitatSuitability
        {
            get
            {
                return habitatSuitability;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> LocalPopulation
        {
            get
            {
                return localPopulation;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> SiteCapacity
        {
            get
            {
                return siteCapacity;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> WeightedBrowse
        {
            get
            {
                return weightedBrowse;
            }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Total Browse To Be Removed
        /// </summary>
        public static ISiteVar<double> TotalBrowse
        {
            get
            {
                return totalBrowse;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<double> BiomassRemoved
        {
            get
            {
                return biomassRemoved;
            }
        }
        //---------------------------------------------------------------------
        public static ISiteVar<int> CohortsPartiallyDamaged
        {
            get
            {
                return cohortsDamaged;
            }
        }
        //---------------------------------------------------------------------
        //public static int GetAddYear(ICohort cohort)
        //{
        //    int currentYear = PlugIn.ModelCore.CurrentTime;
        //    int cohortAddYear = currentYear - cohort.Data.Age;
        //    return cohortAddYear;
        //}

        //---------------------------------------------------------------------
    }
}
