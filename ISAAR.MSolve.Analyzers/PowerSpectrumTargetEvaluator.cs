﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.PreProcessor.Interfaces;
using ISAAR.MSolve.PreProcessor;

namespace ISAAR.MSolve.Analyzers
{
    public class PowerSpectrumTargetEvaluatorCoefficientsProvider : IStochasticMaterialCoefficientsProvider, IStochasticCoefficientsProvider
    {
        private readonly double b;
        private readonly double spectrumStandardDeviation, cutoffError, frequencyIncrement, tolerance;
        private readonly int nPhi, frequencyIntervals, nptsVRF;
        private readonly DOFType randomFieldDirection;
        private double[] sffTarget, omegas;
        private double wu, period;
        private int nptsSff, frequencyCounter;
        private double[] randomVariables = new double[0];

        public PowerSpectrumTargetEvaluatorCoefficientsProvider(double b, double spectrumStandardDeviation, double cutoffError, int nPhi, int nptsVRF, DOFType randomFieldDirection,
            double frequencyIncrement = 0.1, int frequencyIntervals = 256, double tolerance = 1e-10)
        {
            this.b = b;
            this.spectrumStandardDeviation = spectrumStandardDeviation;
            this.cutoffError = cutoffError;
            this.nPhi = nPhi;
            this.randomFieldDirection = randomFieldDirection;
            this.frequencyIncrement = frequencyIncrement;
            this.frequencyIntervals = frequencyIntervals;
            this.nptsVRF = nptsVRF;
            this.tolerance = tolerance;
            Calculate();
        }

        public double SpectrumStandardDeviation { get { return spectrumStandardDeviation; } }
        public double[] SffTarget { get { return sffTarget; } }
        public double[] Omegas { get { return omegas; } }
        public double Wu { get { return wu; } }
        public double Period { get { return period; } }
        public int NPtsSff { get { return nptsSff; } }
        public int NPtsVRF { get { return nptsVRF; } }
        public int FrequencyIntervals { get { return frequencyIntervals; } }
        public int NPhi { get { return nPhi; } }
        public int CurrentMCS { get; set; }
        public int CurrentFrequency { get; set; }

        private double AutoCorrelation(double tau)
        {  
            return Math.Pow(spectrumStandardDeviation, 2) * Math.Exp(-Math.Abs(tau)/b);
        }

        private double SpectralDensity(double omega)
        {
            return Math.Pow(spectrumStandardDeviation, 2) * b / (Math.PI * (1 + Math.Pow(b, 2) * Math.Pow(omega, 2)));
        }

        private void Calculate()
        {
            //const int N = 128;
            double integral = AutoCorrelation(0) / 2d;
            
            frequencyCounter = 1;
            double cumulativeSum = 0;
            double trapezoidArea = 0;

            while (cumulativeSum < (1d - cutoffError) * integral)
            {
                trapezoidArea = 0.5 * (SpectralDensity((frequencyCounter - 1) * frequencyIncrement) + SpectralDensity(frequencyCounter * frequencyIncrement)) * frequencyIncrement;
                if (trapezoidArea < tolerance)
                    break;

                cumulativeSum += trapezoidArea;
                frequencyCounter++;
            }

            wu = frequencyCounter * frequencyIncrement;
            sffTarget = new double[frequencyIntervals];
            omegas = new double[frequencyIntervals];
            double dw = wu / (double)frequencyIntervals;

            //omega = dw/2:dw:wu;
            //Sff_target = feval(spectralDensityFunction,omega);
            for (int i = 0; i < frequencyIntervals; i++)
            {
                omegas[i] = dw / 2 + i * dw;
                sffTarget[i] = SpectralDensity(dw / 2 + i * dw);
            }
            period = 2d * Math.PI / dw;
            nptsSff = (int)(period * wu / Math.PI);
        }

        public double GetCoefficient(double meanValue, double[] coordinates)
        {
            //var Sff = sffTarget;
            //var ku = wu;
            //var Std = spectrumStandardDeviation;
            //var T = period;
            //var n_wu = 20;
            //var Npts = 30;
            var dw = wu / (double)nptsVRF;

            return meanValue * (1 + (Math.Sqrt(2) * spectrumStandardDeviation * 
                Math.Cos((dw / 2 + CurrentFrequency * dw) * coordinates[(int)randomFieldDirection - 1] + 
                (CurrentMCS * 2d * Math.PI / (double)nPhi + 2d * Math.PI / (2d * nPhi)))));

            //w = dw/2:dw:wu;
            ////omega = dw/2:dw:wu;
            ////Sff_target = feval(spectralDensityFunction,omega);
            //for (int i = 0; i < frequencyIntervals; i++)
            //    sffTarget[i] = SpectralDensity(dw / 2 + i * dw);

            //% 2D-Plate   
            //Lx = 1;

            //num_ele_x = 20;
            //num_ele_y = 20;
            //num_ele = num_ele_x*num_ele_y;
            //Le_x = Lx/num_ele_x;
            //for i = 1:num_ele_x
            //    midpoints(i) = Le_x/2+(i-1)*Le_x;
            //end

            //for i = 1:num_ele_y
            //    mid_nodes((i-1)*num_ele_x+1:num_ele_x*i) = midpoints;
            //end

            //n_phi = 10;
            //partitionMatrix = [1:n_phi];  
            //PhaseAnglesMatrix = (partitionMatrix - 1) * 2.*pi / n_phi + 2.*pi / (2 * n_phi);
            //phaseAnglesMatrixSize = size(PhaseAnglesMatrix);
            //FrequencyIntervals = size(w,2);
  
            //// Fast Monte Carlo
            //for frequencyCounter = 1:FrequencyIntervals
            //    for c = 1:n_phi
            //        r = 0;
            //        for xCounter = 1:size(mid_nodes,2)
            //            r = r+1;   
            //            v = mid_nodes(xCounter);
            //            sampleFunctionSet(frequencyCounter,c,r) = sqrt(2.)*Std * cos(w(frequencyCounter) * v + PhaseAnglesMatrix(1, c));
            //        end
            //    end
            //end

            //savefile = 'VRF.mat';
            //save(savefile, 'sampleFunctionSet','ku','dw','Sff','w','Std','T')
        }

        public double[] RandomVariables
        {
            get { return randomVariables; }
            set
            {
                //changedVariables = true;
                randomVariables = value;
            }
        }
    }
}
