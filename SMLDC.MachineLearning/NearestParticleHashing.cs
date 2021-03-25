using SMLDC.Simulator.DiffEquations.Models;
using SMLDC.Simulator.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMLDC.MachineLearning
{

    /*
     *  Idea: divide parameter space into bins (based on dividing the parameter axes into segments)
     *  Use this 'hashing' system for detecting if a certain region (bin) in parameter space is already
     *  occupied by a particle(hypothesis).
     *  Doing a regular nearest neigbour like search is too costly, because you have to compare with all
     *  other particles (over the entire run, because we also want to know if a previous subpopulation
     *  has been exploring in a certain region).
     *  
     *  Similar to the idea of spatial partitioning.
     */
     
    class NodeForNearestParticleHashing
    {
        public int subID;
        private int id;
        private static int teller = 0;
        public NodeForNearestParticleHashing(int subID)
        {
            this.subID = subID;
            id = teller;
            teller++;
        }
        public override string ToString()
        {
            return "Node#" + id + "(sub# " + subID + ")";
        }
        // moeten we hier info opslaan over model? Of is plek in bins al voldoende? 
        // waarschijnlijk wel

        // backlinks naar bins???
    }

    public class NearestParticleHashing
    {
        double[,] lower_higher_bounds_in_logspace; //todo: ragged maken ivm snelheid?
        int nr_bins_per_dimensie;
        List<NodeForNearestParticleHashing>[][] hash_bins;
        private uint nr_dimensies;
        private ParticleFilter particleFilter;
        public NearestParticleHashing(ParticleFilter particlefilter) 
        {
            this.particleFilter = particlefilter;
            initWaarde = (int) particlefilter.settingsForParticleFilter.nearestParticleHashing_initTerugtelWaarde;
            nr_bins_per_dimensie = (int) particlefilter.settingsForParticleFilter.nearestParticleHashing_nr_bins_per_dimension;
            if (nr_bins_per_dimensie > 0)
            {
                double[,] lower_higher_bounds_orig = BergmanAndBretonModel.Get_LOWER_HIGHER_BOUNDS();
                lower_higher_bounds_in_logspace = new double[lower_higher_bounds_orig.GetLength(0), lower_higher_bounds_orig.GetLength(1)];
                for (int i = 0; i < lower_higher_bounds_in_logspace.GetLength(0); i++)
                {
                    for (int j = 0; j < lower_higher_bounds_in_logspace.GetLength(1); j++)
                    {
                        lower_higher_bounds_in_logspace[i, j] = Math.Log10(lower_higher_bounds_orig[i, j]);
                    }
                }
                nr_dimensies = BergmanAndBretonModel.GetNrOfParameters(particlefilter.settingsForParticleFilter.MLUseBretonActivityModel);
                hash_bins = new List<NodeForNearestParticleHashing>[(int)nr_dimensies][];
                for (int i = 0; i < hash_bins.Length; i++)
                {
                    hash_bins[i] = new List<NodeForNearestParticleHashing>[nr_bins_per_dimensie];
                    //for (int j = 0; j < hash_bins[i].Length; j++)
                    //{
                    //    // new will be 'lazy' JiT.
                    //    //hash_bins[i][j] = new List<NodeForNearestParticleHashing>();
                    //}
                }
            }
        }
    
       


        private string Represent(List<NodeForNearestParticleHashing> lijst, bool getal)
        {
            int x = 0;
            if (lijst != null)
            {
                x = lijst.Count;
            }
            if (getal)
            {
                int nr_chars = 7;
                if (lijst == null)
                {
                    string s = " ";
                    while (s.Length < nr_chars)
                    {
                        s = s + " ";
                    }
                    return s;
                }
                return OctaveStuff.MyFormat(x, nr_chars);
            }

            if(lijst == null) { return " "; }
            if(x == 0)
            {
                return  " ";
            }
            else if (x <= (int)(0.1* initWaarde) )
            {
                return  ".";
            }
            else if (x <= (int)(0.3 * initWaarde) )
            {
                return "+";
            }
            else if (x <= (int)(0.5 * initWaarde))
            {
                return  "c";
            }
            else if (x <= (int)(0.7 * initWaarde))
            {
                return "o";
            }
            else if (x <= (int)(0.95 * initWaarde))
            {
                return "0";
            }
            else
            {
                return "@";
            }
        }


        public void Print()
        {
            if (nr_bins_per_dimensie <= 0)
            {
                Console.WriteLine(" ===== NearestParticleHashing disabled =====");
                return;
            }

            Console.WriteLine(" ===== NearestParticleHashing (calls: " + (bezet_counter + vrij_counter) + ", fraction occupied: " + getBezetFractie() + ", " + nrSamplesGevraagd+ " samples, resamples: " + nrResamples + ") =====");
            for (int dim = 0; dim < hash_bins.Length; dim++)
            {
                Console.Write("| ");
                double laagsteLog = lower_higher_bounds_in_logspace[dim, 0];
                double hoogsteLog = lower_higher_bounds_in_logspace[dim, 1];
                double aantal = 0;
                double som = 0;
                int max = 0;
                for (int j = 0; j < hash_bins[dim].Length; j++)
                {
                    //                    Console.Write(OctaveStuff.MyFormat(hash_bins[i][j], 4));
                    // bin centrum terugrekenen naar 'originele' model param.
                    double fractieInLog = (j + 0.5) / (double)nr_bins_per_dimensie;
                    double deltaInLog = fractieInLog * (hoogsteLog - laagsteLog);
                    double logValue = laagsteLog + deltaInLog;
                    if (hash_bins[dim][j] != null)
                    {
                        som += logValue * hash_bins[dim][j].Count;
                        aantal += hash_bins[dim][j].Count;
                        if (hash_bins[dim][j].Count > max)
                        {
                            max = hash_bins[dim][j].Count;
                        }
                    }
                    Console.Write(OctaveStuff.MyFormat(Math.Pow(10, logValue)) + " ");
                }
                double gewogenWaarde = Math.Pow(10, som / aantal);

                Console.Write(" | ");
                initWaarde = max; // hack om te schalen
                for (int j = 0; j < hash_bins[dim].Length; j++)
                {
                    Console.Write(Represent(hash_bins[dim][j], false));
                }
                Console.Write(" | ");
                for (int j = 0; j < hash_bins[dim].Length; j++)
                {
                    Console.Write(Represent(hash_bins[dim][j], true));
                }

                Console.WriteLine(" | " + OctaveStuff.MyFormat(BergmanAndBretonModel.GetParameterName(true, dim), 8) + " ~~~> " + OctaveStuff.MyFormat(gewogenWaarde));
            }
        }




        public double[] GetWeightedEstimates()
        {
            if (nr_bins_per_dimensie <= 0)
            {
                return null;
            }
            double[] estimates = new double[nr_dimensies];
            for (int dim = 0; dim < nr_dimensies; dim++)
            {
                //Console.Write(OctaveStuff.MyFormat(BergmanAndBretonModel.GetParameterName(true, dim), 10) + "|  ");
                double laagsteLog = lower_higher_bounds_in_logspace[dim, 0];
                double hoogsteLog = lower_higher_bounds_in_logspace[dim, 1];
                double aantal = 0;
                double somInLogspace = 0;
                for (int j = 0; j < nr_bins_per_dimensie; j++)
                {
                    if (hash_bins[dim][j] != null)
                    {
                        double fractieInLog = (j + 0.5) / (double)nr_bins_per_dimensie;
                        double deltaInLog = fractieInLog * (hoogsteLog - laagsteLog);
                        double logValue = laagsteLog + deltaInLog;
                        somInLogspace += logValue * hash_bins[dim][j].Count;
                        aantal += hash_bins[dim][j].Count;
                    }
                }
                double gewogenWaardeLogspace = somInLogspace / aantal;
                double gewogenWaarde = Math.Pow(10, gewogenWaardeLogspace);
                estimates[dim] = gewogenWaarde;
            }
            return estimates;
        }



        private double getBezetFractie() {
            if(vrij_counter == 0 && bezet_counter == 0 ) { return 0; }
            return bezet_counter / (double)(vrij_counter + bezet_counter);
        }

        private readonly object bezet_lockobject = new object();
        private int bezet_counter = 0;
        private int vrij_counter = 0;

        public bool IsBezet(Particle particle)
        {
            if (nr_bins_per_dimensie <= 0)
            {
                return false;
            }
            BergmanAndBretonModel model = particle.model;
            return IsBezet(model, particle.subPopulatie.ID);
        }
        public bool IsBezet(BergmanAndBretonModel model, int subID)
        {
            if (nr_bins_per_dimensie <= 0)
            {
                return false;
            }
            // zoek uit in elke dimensie of er nodes zijn voor deze bin.
            // alleen als die nodes in elke dimensie in die in zitten, horen ze thuis in botsendeNodes.
            HashSet<NodeForNearestParticleHashing> botsendeNodes = null; // = new HashSet<NodeForNearestParticleHashing>();
            for (int dim = 0; dim < nr_dimensies; dim++)
            {
                double paramvalue = model.GetParameter(dim);
                double logValue = Math.Log10(paramvalue);
                double laagsteLog = lower_higher_bounds_in_logspace[dim, 0];
                double hoogsteLog = lower_higher_bounds_in_logspace[dim, 1];
                double deltaInLog = logValue - laagsteLog;
                double fractieInLog = deltaInLog / (hoogsteLog - laagsteLog);
                // converteer naar bin_index:
                int bin_ndx = (int)(nr_bins_per_dimensie * fractieInLog); // floor
                bin_ndx = MyMath.Clip(bin_ndx, 0, nr_bins_per_dimensie - 1);

                List<NodeForNearestParticleHashing> deze_bin_lijst = hash_bins[dim][bin_ndx];
                if (deze_bin_lijst == null)
                {
                    // niemand hier, dus sowieso nog vrij
                    break;
                }
                else
                {
                    if (dim == 0)
                    {
                        botsendeNodes = new HashSet<NodeForNearestParticleHashing>(deze_bin_lijst);
                    }
                    else
                    {
                        botsendeNodes.IntersectWith(deze_bin_lijst);
                        if (botsendeNodes.Count <= 1)
                        {
                            // Dit ben je alleen nog maar zelf? toch?
                            foreach (NodeForNearestParticleHashing key in botsendeNodes)
                            {
                                if (key.subID == subID)
                                {
                                    break;
                                }
                            }
                            // er zijn geen kandidaten meer!
                            //break;
                        }
                    }

                }
            }
            if (botsendeNodes != null && botsendeNodes.Count > 0)
            {
                foreach (NodeForNearestParticleHashing key in botsendeNodes)
                {
                    if (key.subID != subID)
                    {
                        lock (bezet_lockobject)
                        {
                            bezet_counter++;
                            return true;
                        }
                    }
                }
                //alleen jezelf tegengekomen, en dat is natuurlijk niet 'bezet'
            }
            lock (bezet_lockobject)
            {
                vrij_counter++;
            }
            return false;
        }


        public int[] RegistreerParticle(Particle particle)
        {
            if (nr_bins_per_dimensie <= 0)
            {
                return null;
            }
            if(particle.subPopulatie == null) { return null; } //sorry, we currently cannot register you (node gets subpop id... which you don't have)

            int[] indices = GetBinIndices(particle);
            NodeForNearestParticleHashing node = new NodeForNearestParticleHashing(particle.subPopulatie.ID);
            for (int dim = 0; dim < nr_dimensies; dim++)
            {
                int bin_ndx = indices[dim];
                List<NodeForNearestParticleHashing> lijst_om_aan_toe_te_voegen = null;
                if (hash_bins[dim][bin_ndx] == null)
                {
                    lijst_om_aan_toe_te_voegen = new List<NodeForNearestParticleHashing>();
                    hash_bins[dim][bin_ndx] = lijst_om_aan_toe_te_voegen;
                }
                else
                {
                    lijst_om_aan_toe_te_voegen = hash_bins[dim][bin_ndx];
                }
                lijst_om_aan_toe_te_voegen.Add(node);
                // buren  ook nog?               
            }
            return indices;
        }


        public int initWaarde;
        //return de bin indices
        public int[] GetBinIndices(Particle particle)
        {
            if (nr_bins_per_dimensie <= 0)
            {
                return null;
            }
            // zoek uit welke bin op elke dimensie
            int[] bin_indices = new int[nr_dimensies];
            for(int dim = 0; dim < nr_dimensies; dim++)
            {
                double paramvalue = particle.model.GetParameter(dim);
                double logValue = Math.Log10(paramvalue);
                double laagsteLog = lower_higher_bounds_in_logspace[dim, 0];
                double hoogsteLog = lower_higher_bounds_in_logspace[dim, 1];
                double deltaInLog = logValue - laagsteLog;
                double fractieInLog = deltaInLog / (hoogsteLog - laagsteLog);

                // converteer naar bin_index:
                int bin_ndx = (int) (nr_bins_per_dimensie * fractieInLog); // floor
                bin_ndx = MyMath.Clip(bin_ndx, 0, nr_bins_per_dimensie - 1);
                bin_indices[dim] = bin_ndx;
            }
            return bin_indices;
        }

        // find a new bergman model in an area that is not occupied yet, but close to the model of the particle.
        private int nrSamplesGevraagd = 0;
        private int nrResamples = 0;
        public BergmanAndBretonModel GetRandomSample(RandomStuff random, Particle particle)
        {
            //double[] estimates = GetWeightedEstimates();
            double factor = 0.5 / particleFilter.settingsForParticleFilter.nearestParticleHashing_nr_bins_per_dimension;
            double[,] randomRangeForParams = new double[particle.model.GetNrOfParameters(), 2];
            for(int paramNdx = 0; paramNdx < randomRangeForParams.GetLength(0); paramNdx++)
            {
                double value = particle.model.GetParameter(paramNdx);
                double logValue = Math.Log10(value);
                // in log10 space optellen, dan naar normale space terugrekenen
                randomRangeForParams[paramNdx, 0] = MyMath.Clip(Math.Pow(10, logValue - factor * particleFilter.SIGMA_STEP_SIZES_logspace[paramNdx]), BergmanAndBretonModel.GetLowerBound(paramNdx), BergmanAndBretonModel.GetHigherBound(paramNdx) );
                randomRangeForParams[paramNdx, 1] = MyMath.Clip( Math.Pow(10, logValue + factor * particleFilter.SIGMA_STEP_SIZES_logspace[paramNdx]), BergmanAndBretonModel.GetLowerBound(paramNdx), BergmanAndBretonModel.GetHigherBound(paramNdx) );

            }
            BergmanAndBretonModel model = new BergmanAndBretonModel(particleFilter.settingsForParticleFilter.MLUseBretonActivityModel);
            model.SetParameters_RandomInLogRange(random, randomRangeForParams);
            lock (bezet_lockobject)
            {
                nrSamplesGevraagd++;
            }
            
           // Console.WriteLine(" ######################### NearestParticleHashing.randomSample...      ##########################");
            while (IsBezet(model, -1))
            {
              //  Console.WriteLine(" ######################### NearestParticleHashing.randomSample...bezet ##########################");
                // ga in de buurt van dit (bezette) model zoeken. Zo randomwalken we vanzelf naar een niet bezette plek ergens in parameter space
                for (int paramNdx = 0; paramNdx < randomRangeForParams.GetLength(0); paramNdx++)
                {
                    double value = model.GetParameter(paramNdx);
                    double logValue = Math.Log10(value);
                    // in log10 space optellen, dan naar normale space terugrekenen
                    randomRangeForParams[paramNdx, 0] = MyMath.Clip(Math.Pow(10, logValue - factor * particleFilter.SIGMA_STEP_SIZES_logspace[paramNdx]), BergmanAndBretonModel.GetLowerBound(paramNdx), BergmanAndBretonModel.GetHigherBound(paramNdx));
                    randomRangeForParams[paramNdx, 1] = MyMath.Clip(Math.Pow(10, logValue + factor * particleFilter.SIGMA_STEP_SIZES_logspace[paramNdx]), BergmanAndBretonModel.GetLowerBound(paramNdx), BergmanAndBretonModel.GetHigherBound(paramNdx));
                }
                model = new BergmanAndBretonModel(particleFilter.settingsForParticleFilter.MLUseBretonActivityModel);
                model.SetParameters_RandomInLogRange(random, randomRangeForParams);
                lock (bezet_lockobject)
                {
                    nrResamples++;
                }
            }
            return model;
        }
    }

}

