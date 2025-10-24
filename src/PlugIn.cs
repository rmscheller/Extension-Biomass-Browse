//  Authors:  Brian Miranda, Nate De Jager, Patrick Drohan, Robert Scheller

using Landis.SpatialModeling;
using Landis.Core;
using System.Collections.Generic;
using Landis.Library.Metadata;
using System;
using System.Dynamic;
using Landis.Library.UniversalCohorts;


namespace Landis.Extension.Browse
{
    ///<summary>
    /// A disturbance plug-in that simulates browsing by ungulates.
    /// </summary>

    public class PlugIn
        : ExtensionMain
    {
        public static readonly ExtensionType ExtType = new ExtensionType("disturbance:browse");
        public static readonly string ExtensionName = "Biomass Browse";

        public static MetadataTable<EventsLog> eventLog;
        public static MetadataTable<SummaryLog> summaryLog;
        public static MetadataTable<CalibrateLog> calibrateLog;
        public static MetadataTable<EventsSpeciesLog> eventSpeciesLog;
        public static bool Calibrate = false;

        private string sitePrefMapNameTemplate;
        private string siteForageMapNameTemplate;
        private string siteHSIMapNameTemplate;
        private string sitePopMapNamesTemplate;
        private string biomassRemovedMapNameTemplate;
        private IInputParameters parameters;

        //Which version of the population model to use? Static population, dynamic population, or BDI
        public static bool DynamicPopulation = false; //SF changed this so that static population can happen -- otherwise
                                                      //dynamic is always used
        public static bool UseBDI = false;
        public static string PropInReachMethod = "Ordered";

        //Dynamic Population Parameters
        public static double PopRMin;
        public static double PopRMax;
        public static double PopMortalityMin;
        public static double PopMortalityMax;
        public static double PopHarvestMin;
        public static double PopHarvestMax;
        public static double PopPredationMin;
        public static double PopPredationMax;
        private bool addCohortData = false;


        //---------------------------------------------------------------------

        public PlugIn()
            : base(ExtensionName, ExtType)
        {
        }

        //---------------------------------------------------------------------

        public static ICore ModelCore { get; private set; }
        public override void AddCohortData()
        {
            //dynamic tempObject = additionalCohortParameters;
            //tempObject.ForageInReach = 0.0f;
            //tempObject.ProportionBrowse = 0.0f;
            //tempObject.Forage = 0.0f;
            //tempObject.BiomassRemoval = 0.0f;

            ExpandoObject addFields = new ExpandoObject();
            dynamic tempObject = addFields;
            tempObject.ForageInReach = 0.0f;
            tempObject.ProportionBrowse = 0.0f;
            tempObject.Forage = 0.0f;
            tempObject.BiomassRemoval = 0.0f;
            SiteVars.SetAdditionalFields(addFields);


        }
        private void AddCohortDataDisturbance()
        {
            foreach (ActiveSite site in ModelCore.Landscape)
            {
                var siteCohorts = SiteVars.Cohorts[site];
                foreach (var speciesCohort in siteCohorts)
                {
                    foreach (var cohort in speciesCohort)
                    {
                        dynamic tempObject = cohort.Data.AdditionalParameters;
                        tempObject.ForageInReach = 0.0f;
                        tempObject.ProportionBrowse = 0.0f;
                        tempObject.Forage = 0.0f;
                        tempObject.BiomassRemoval = 0.0f;
                    }
                }
            }
        }
        //---------------------------------------------------------------------

        public override void LoadParameters(string dataFile, ICore mCore)
        {
            ModelCore = mCore;
            InputParameterParser parser = new InputParameterParser();
            parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);

        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes the plug-in with a data file.
        /// </summary>
        /// <param name="dataFile">
        /// Path to the file with initialization data.
        /// </param>
        /// <param name="startTime">
        /// Initial timestep (year): the timestep that will be passed to the
        /// first call to the component's Run method.
        /// </param>
        public override void Initialize()
        {
            Timestep = parameters.Timestep;
            sitePrefMapNameTemplate = parameters.SitePrefMapNamesTemplate;
            siteForageMapNameTemplate = parameters.SiteForageMapNamesTemplate;
            siteHSIMapNameTemplate = parameters.SiteHSIMapNamesTemplate;
            sitePopMapNamesTemplate = parameters.SitePopMapNamesTemplate;
            biomassRemovedMapNameTemplate = parameters.BiomassRemovedMapNamesTemplate;

            //Set up empty siteVars and dictionaries for Forage, ForageInReach, and LastBrowseProportion
            SiteVars.Initialize();

            PopulationZones.ReadMap(parameters.ZoneMapFileName);

            DynamicInputs.Initialize(parameters.PopulationFileName, false, parameters);

            parameters.PreferenceList =  PreferenceList.Initialize(parameters.SppParameters);

            //BrowseDisturbance.Initialize();
            GrowthReduction.Initialize(parameters);
            //Defoliate.Initialize(parameters); //SF be careful if using defoliate -- forage could be greater than available leaf biomass, and browsing
                                                // could be double-counted if using both defoliate and BiomassCohorts.ReduceOrKillMarkedCohorts
            PopulationZones.Initialize(parameters);
            
            //This is used when calculating habitat suitability
            parameters.ForageNeighbors = GetResourceNeighborhood(parameters.ForageQuantityNbrRad);
            parameters.SitePrefNeighbors = GetResourceNeighborhood(parameters.SitePrefNbrRad);
                     
            ModelCore.UI.WriteLine("   Opening browse log files \"{0}\" ...", parameters.LogFileName); //TODO SF this filename isn't used
            MetadataHandler.InitializeMetadata(Timestep);
        }

        //---------------------------------------------------------------------

        ///<summary>
        /// Run the plug-in at a particular timestep.
        ///</summary>
        public override void Run()
        {
            //running = true;
            ModelCore.UI.WriteLine("Processing landscape for ungulate browse events ...");

            if (ModelCore.CurrentTime > 0)
            {
                if (addCohortData == false)
                {
                    AddCohortDataDisturbance();
                    addCohortData = true;
                }
            }

            //This does everything -- calculates forage and disturbs sites
            Event browseEvent = Event.Initiate(parameters);

            if (browseEvent != null)
                LogBrowseEvent(browseEvent);

            //calibrate log write
            if(PlugIn.Calibrate)
                CalibrateLog.WriteLogFile(PlugIn.ModelCore.CurrentTime);
            
            //  Write site preference map 
            string path = MapNames.ReplaceTemplateVars(sitePrefMapNameTemplate, PlugIn.ModelCore.CurrentTime);
            Dimensions dimensions = new Dimensions(ModelCore.Landscape.Rows, ModelCore.Landscape.Columns);
            using (IOutputRaster<ShortPixel> outputRaster = ModelCore.CreateRaster<ShortPixel>(path, dimensions))
            {
                ShortPixel pixel = outputRaster.BufferPixel;
                foreach (Site site in PlugIn.ModelCore.Landscape.AllSites) {
                    if (site.IsActive) {
                            pixel.MapCode.Value = (short) (SiteVars.SitePreference[site] * 100);
                    }
                    else {
                        //  Inactive site
                        pixel.MapCode.Value = 0;
                    }
                    outputRaster.WriteBufferPixel();
                }
            }

            //  Write site forage map 
            path = MapNames.ReplaceTemplateVars(siteForageMapNameTemplate, PlugIn.ModelCore.CurrentTime);
            dimensions = new Dimensions(ModelCore.Landscape.Rows, ModelCore.Landscape.Columns);
            using (IOutputRaster<ShortPixel> outputRaster = ModelCore.CreateRaster<ShortPixel>(path, dimensions))
            {
                ShortPixel pixel = outputRaster.BufferPixel;
                foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                    {
                        pixel.MapCode.Value = (short)(SiteVars.ForageQuantity[site]);
                    }
                    else
                    {
                        //  Inactive site
                        pixel.MapCode.Value = 0;
                    }
                    outputRaster.WriteBufferPixel();
                }
            }

            //  Write browse removed map 
            path = MapNames.ReplaceTemplateVars(biomassRemovedMapNameTemplate, PlugIn.ModelCore.CurrentTime);
            dimensions = new Dimensions(ModelCore.Landscape.Rows, ModelCore.Landscape.Columns);
            using (IOutputRaster<ShortPixel> outputRaster = ModelCore.CreateRaster<ShortPixel>(path, dimensions))
            {
                ShortPixel pixel = outputRaster.BufferPixel;
                foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                    {
                        pixel.MapCode.Value = (short)(SiteVars.TotalBrowse[site]);
                    }
                    else
                    {
                        //  Inactive site
                        pixel.MapCode.Value = 0;
                    }
                    outputRaster.WriteBufferPixel();
                }
            }
            //  Write site population
            path = MapNames.ReplaceTemplateVars(sitePopMapNamesTemplate, PlugIn.ModelCore.CurrentTime);
            dimensions = new Dimensions(ModelCore.Landscape.Rows, ModelCore.Landscape.Columns);
            
            using (IOutputRaster<ShortPixel> outputRaster = ModelCore.CreateRaster<ShortPixel>(path, dimensions))
            {
                ShortPixel pixel = outputRaster.BufferPixel;
                foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                    {
                        //LocalPopulation is #of deer not density; need to convert to density to map
                        double popDens = SiteVars.LocalPopulation[site] / (ModelCore.CellLength * ModelCore.CellLength) * 1000 * 1000;

                        //PlugIn.ModelCore.UI.WriteLine("popDens is {0} individuals per square km.", popDens);//debug

                        // Mult by 100 and round to integer for mapping
                        pixel.MapCode.Value = (short)(popDens * 100);

                    }
                    else
                    {
                        //  Inactive site
                        pixel.MapCode.Value = 0;
                    }
                    outputRaster.WriteBufferPixel();
                }
            }

            //  Write site HSI
            path = MapNames.ReplaceTemplateVars(siteHSIMapNameTemplate, PlugIn.ModelCore.CurrentTime);
            dimensions = new Dimensions(ModelCore.Landscape.Rows, ModelCore.Landscape.Columns);
            using (IOutputRaster<ShortPixel> outputRaster = ModelCore.CreateRaster<ShortPixel>(path, dimensions))
            {
                ShortPixel pixel = outputRaster.BufferPixel;
                foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                    {
                        pixel.MapCode.Value = (short)(SiteVars.HabitatSuitability[site] * 100);
                    }
                    else
                    {
                        //  Inactive site
                        pixel.MapCode.Value = 0;
                    }
                    outputRaster.WriteBufferPixel();
                }
            }
        }

        //---------------------------------------------------------------------
        private void LogBrowseEvent(Event browseEvent)
        {
            //calculate totals for entire landscape
            int totalPopulation = 0;
            int totalK = 0;
            int totalEffPop = 0;
            long totalForage = 0;
            int totalSitesDamaged = 0;
            int totalBiomassRemoved = 0;
            int totalBiomassKilled = 0;
            int totalCohortsKilled = 0;
            int totalSites = 0;

            foreach (PopulationZone popZone in PopulationZones.Dataset)
            {
                totalPopulation += (int)popZone.Population;
                totalK += (int) popZone.K;
                totalEffPop += (int) popZone.EffectivePop;
                totalForage += (int) popZone.TotalForage;
                totalSitesDamaged += browseEvent.ZoneSitesDamaged[popZone.Index];
                totalBiomassRemoved += (int) browseEvent.ZoneBiomassRemoved[popZone.Index];
                totalBiomassKilled += (int) browseEvent.ZoneBiomassKilled[popZone.Index];
                totalCohortsKilled += browseEvent.ZoneCohortsKilled[popZone.Index];
                totalSites += PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count;
            }

            PlugIn.summaryLog.Clear();
            SummaryLog sl = new SummaryLog();
            sl.Time = ModelCore.CurrentTime;
            sl.TotalSites = totalSites;
            sl.TotalPopulation = totalPopulation;
            sl.TotalSitesDamaged = totalSitesDamaged;
            sl.PopulationDensity = (double) ((double) totalPopulation / (double) totalSites / Math.Pow(PlugIn.ModelCore.CellLength, 2.0) * 1000000.0); //population density (km-2)
            sl.TotalForage = totalForage; //kg
            sl.MeanForage = (int)((totalForage * 1000) / (totalSites * Math.Pow(PlugIn.ModelCore.CellLength, 2.0))); //g/m2
            sl.TotalK = totalK;
            sl.TotalEffectivePopulation = totalEffPop;
            sl.AverageBiomassRemoved = totalBiomassRemoved / totalSites; //site mean biomass in g/m2
            sl.AverageBiomassKilled = totalBiomassKilled / totalSites; //site mean biomass in g/m2
            sl.TotalCohortsKilled = totalCohortsKilled;
            sl.BDI = (totalBiomassRemoved * Math.Pow(PlugIn.ModelCore.CellLength, 2.0)) / (totalForage*1000);

            summaryLog.AddObject(sl);
            summaryLog.WriteToFile();


            foreach (IPopulationZone popZone in PopulationZones.Dataset)
            {
                PlugIn.eventLog.Clear();
                EventsLog el = new EventsLog();

                el.Time = ModelCore.CurrentTime;
                el.TotalSites = PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count;
                el.PopulationZone = PopulationZones.Dataset[popZone.Index].MapCode;
                el.TotalPopulation = (int)PopulationZones.Dataset[popZone.Index].Population;
                el.TotalSitesDamaged = browseEvent.ZoneSitesDamaged[popZone.Index];
                el.PopulationDensity = (double)((double)PopulationZones.Dataset[popZone.Index].Population / 
                    (double)PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count *
                    (1000000.0 / Math.Pow(PlugIn.ModelCore.CellLength, 2.0))); //population density (km-2)
                el.TotalForage = (long) PopulationZones.Dataset[popZone.Index].TotalForage; //kg
                el.MeanForage = (int)((PopulationZones.Dataset[popZone.Index].TotalForage * 1000) / 
                    (PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count * Math.Pow(PlugIn.ModelCore.CellLength, 2.0))); //g/m2 
                el.TotalK = (int) PopulationZones.Dataset[popZone.Index].K;
                el.TotalEffectivePopulation = (int) PopulationZones.Dataset[popZone.Index].EffectivePop;
                el.AverageBiomassRemoved = (int) (browseEvent.ZoneBiomassRemoved[popZone.Index] /
                                    (double)PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count); //site mean biomass in g/m2
                el.AverageBiomassKilled = (int) (browseEvent.ZoneBiomassKilled[popZone.Index] /
                                    (double)PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count); //site mean biomass in g/m2
                el.TotalCohortsKilled = browseEvent.ZoneCohortsKilled[popZone.Index];
                el.BDI = (browseEvent.ZoneBiomassRemoved[popZone.Index] * Math.Pow(PlugIn.ModelCore.CellLength, 2.0)) /(el.TotalForage*1000);

                eventLog.AddObject(el);
                eventLog.WriteToFile();
            }

            foreach (IPopulationZone popZone in PopulationZones.Dataset)
            {
                foreach (ISpecies species in PlugIn.ModelCore.Species)
                {
                    PlugIn.eventSpeciesLog.Clear();
                    EventsSpeciesLog el = new EventsSpeciesLog();
                    el.Time = ModelCore.CurrentTime;
                    el.PopulationZone = PopulationZones.Dataset[popZone.Index].MapCode;
                    el.TotalSites = PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count;
                    el.SpeciesName = species.Name;
                    el.SpeciesIndex = species.Index;
                    el.AverageForage = browseEvent.ZoneForageSpp[popZone.Index][species.Index] /
                        (double)PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count;
                    el.AverageForageInReach = browseEvent.ZoneForageInReachSpp[popZone.Index][species.Index] /
                        (double)PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count;
                    el.AverageBiomassBrowsed = (double)(browseEvent.ZoneBiomassBrowsedSpp[popZone.Index][species.Index] /
                       (double)PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count); //site mean biomass in g/m2
                    el.AverageBiomassRemoved = (double)(browseEvent.ZoneBiomassRemovedSpp[popZone.Index][species.Index] /
                        (double)PopulationZones.Dataset[popZone.Index].PopulationZoneSites.Count); //site mean biomass in g/m2
                    el.TotalCohortsKilled = browseEvent.ZoneCohortsKilledSpp[popZone.Index][species.Index];

                    eventSpeciesLog.AddObject(el);
                    eventSpeciesLog.WriteToFile();

                }
            }


        }

        //---------------------------------------------------------------------
        //Generate a Relative Location array (with WEIGHTS) of neighbors.
        //Check each cell within a block surrounding the center point.  This will
        //create a set of POTENTIAL neighbors.  These potential neighbors
        //will need to be later checked to ensure that they are within the landscape
        // and active.

        //TODO SF debug, check that it is working

        private static IEnumerable<RelativeLocationWeighted> GetResourceNeighborhood(double neighborRadius)
        {
            float CellLength = PlugIn.ModelCore.CellLength;
            PlugIn.ModelCore.UI.WriteLine("Creating Neighborhood List.");
            int numCellRadius = (int)(neighborRadius / CellLength);
            PlugIn.ModelCore.UI.WriteLine("NeighborRadius={0}, CellLength={1}, numCellRadius={2}", neighborRadius, CellLength, numCellRadius);

            double centroidDistance = 0;
            double cellLength = CellLength;
            double neighborWeight = 0;

            List<RelativeLocationWeighted> neighborhood = new List<RelativeLocationWeighted>();

            for (int row = (numCellRadius * -1); row <= numCellRadius; row++)
            {
                for (int col = (numCellRadius * -1); col <= numCellRadius; col++)
                {
                    neighborWeight = 0;
                    centroidDistance = DistanceFromCenter(row, col);
                    //PlugIn.ModelCore.Log.WriteLine("Centroid Distance = {0}.", centroidDistance);
                    if (centroidDistance <= neighborRadius && centroidDistance > 0)
                    {

                        //if (parameters.ShapeOfNeighbor == NeighborShape.uniform)
                            neighborWeight = 1.0;
                        /*
                        if (parameters.ShapeOfNeighbor == NeighborShape.linear)
                        {
                            //neighborWeight = (neighborRadius - centroidDistance + (cellLength/2)) / (double) neighborRadius;
                            neighborWeight = 1.0 - (centroidDistance / (double)neighborRadius);
                        }
                        if (parameters.ShapeOfNeighbor == NeighborShape.gaussian)
                        {
                            double halfRadius = neighborRadius / 2;
                            neighborWeight = (float)
                            System.Math.Exp(-1 *
                            System.Math.Pow(centroidDistance, 2) /
                            System.Math.Pow(halfRadius, 2));
                        }
                        */
                        RelativeLocation reloc = new RelativeLocation(row, col);
                        neighborhood.Add(new RelativeLocationWeighted(reloc, neighborWeight));
                    }
                }
            }
            return neighborhood;
        }
        //-------------------------------------------------------
        //Calculate the distance from a location to a center
        //point (row and column = 0).
        private static double DistanceFromCenter(double row, double column)
        {
            double CellLength = PlugIn.ModelCore.CellLength;
            row = System.Math.Abs(row) * CellLength;
            column = System.Math.Abs(column) * CellLength;
            double aSq = System.Math.Pow(column, 2);
            double bSq = System.Math.Pow(row, 2);
            return System.Math.Sqrt(aSq + bSq);
        }
        //---------------------------------------------------------------------
        // Event handler when a cohort is killed by an age-only disturbance.
        //public void CohortKilledByAgeOnlyDisturbance(object sender,
        //                                             DeathEventArgs eventArgs)
        //{
        //    // If this plug-in is not running, then some base disturbance
        //    // plug-in killed the cohort.
        //    if (!running)
        //        return;

        //}
        //---------------------------------------------------------------------
    }

    public class RelativeLocationWeighted
    {
        private RelativeLocation location;
        private double weight;

        //---------------------------------------------------------------------
        public RelativeLocation Location
        {
            get
            {
                return location;
            }
            set
            {
                location = value;
            }
        }

        public double Weight
        {
            get
            {
                return weight;
            }
            set
            {
                weight = value;
            }
        }

        public RelativeLocationWeighted(RelativeLocation location, double weight)
        {
            this.location = location;
            this.weight = weight;
        }

    }

    
}
