﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.XFEM.Utilities;
using ISAAR.MSolve.XFEM.Geometry.CoordinateSystems;
using ISAAR.MSolve.XFEM.LinearAlgebra;

namespace ISAAR.MSolve.XFEM.Enrichments.Items.CrackTip
{
    // Perhaps I should not expose this class, but use it privately in TipCoordinateSystem and have batch methods when 
    // a lot of calls need to calculate the jacobians at the same point.
    class TipJacobians
    {
        private readonly TipCoordinateSystem tipSystem;
        private readonly DenseMatrix inverseJacobianPolarToLocal;
        private readonly DenseMatrix inverseJacobianPolarToGlobal;

        public TipJacobians(TipCoordinateSystem tipSystem, PolarPoint2D polarCoordinates)
        {
            this.tipSystem = tipSystem;

            double r = polarCoordinates.R;
            double cosTheta = Math.Cos(polarCoordinates.Theta);
            double sinTheta = Math.Sin(polarCoordinates.Theta);
            inverseJacobianPolarToLocal = new DenseMatrix(new double[,] 
                { { cosTheta, sinTheta }, {-sinTheta / r , cosTheta / r } });

            inverseJacobianPolarToGlobal = inverseJacobianPolarToLocal * tipSystem.RotationMatrixGlobalToLocal;
        }

        public double[] TransformScalarFieldDerivativesLocalPolarToLocalCartesian(double[] gradient)
        {
            return gradient * inverseJacobianPolarToLocal;
        }

        public double[] TransformScalarFieldDerivativesLocalPolarToGlobalCartesian(double[] gradient)
        {
            return gradient * inverseJacobianPolarToGlobal;
        }

        /// <summary>
        /// Attention: The input vector field is differentiated w.r.t. the polar cartesian system coordinates.
        /// The output vector field is differentiated w.r.t. the local cartesian system coordinates. However the 
        /// representations of both vector fields (aka the coordinates of the vectors) are in the local cartesian 
        /// coordinate system.
        /// </summary>
        /// <param name="gradient">A 2x2 matrix, for which: Row i is the gradient of the ith component of the vector  
        ///     field, thus:    gradient = [Fr,r Fr,theta; Ftheta,r Ftheta,theta],
        ///     where Fi,j is the derivative of component i w.r.t. coordinate j</param>
        /// <returns></returns>
        public DenseMatrix TransformVectorFieldDerivativesLocalPolarToLocalCartesian(DenseMatrix gradient)
        {
            return gradient * inverseJacobianPolarToLocal;
        }
    }
}
