using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VesselSimulator.TFVesselSimulator
{
    public static class Mat3x3
    {
        public static float[] ZERO3F { get { return new float[3] { 0f, 0f, 0f }; } private set { } }
        public static double[] ZERO3D { get { return new double[3] { 0f, 0f, 0f }; } private set { } }

        #region mat3x3

        public static float[,] Matmul(float[,] A, float[,] B)
        {
            float[,] result = new float[3, 3] { { 0f, 0f, 0f }, { 0f, 0f, 0f }, { 0f, 0f, 0f } };
            float sum = 0f;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sum = 0f;
                    for (int k = 0; k < 3; k++)
                        sum = sum + A[i, k] * B[k, j];
                    result[i, j] = sum;
                }
            }
            return result;
        }

        public static float[] Matmul(float[,] A, float[] B)
        {
            float[] result = new float[3];
            float sum = 0f;
            for (int i = 0; i < 3; i++)
            {
                sum = 0f;
                for (int k = 0; k < 3; k++)
                    sum = sum + A[i, k] * B[k];
                result[i] = sum;
            }
            return result;
        }

        public static float[,] Add3x3(float[,] A, float[,] B)
        {
            float[,] result = new float[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result[i, j] = A[i, j] + B[i, j];
            return result;
        }
        public static float[,] Add3x3(float[,] A, float b)
        {
            float[,] result = new float[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result[i, j] = A[i, j] + b;
            return result;
        }
        public static float[,] Multiply3x3(float[,] A, float b)
        {
            float[,] result = new float[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result[i, j] = A[i, j] * b;
            return result;
        }

        public static float Det3x3(float[,] A)
        {
            return A[0, 0] * A[1, 1] * A[2, 2] + A[0, 1] * A[1, 2] * A[2, 0] + A[0, 2] * A[1, 0] * A[2, 1] -
                   A[0, 0] * A[1, 2] * A[2, 1] - A[0, 1] * A[1, 0] * A[2, 2] - A[0, 2] * A[1, 1] * A[2, 0];
        }
        public static float Det2x2(float[,] A)
        {
            return A[0, 0] * A[1, 1] - A[0, 1] * A[1, 0];
        }

        public static float[,] Cofactor(float[,] A)
        {
            return new float[3, 3]
            {
            { Det2x2(new float[2,2]{ { A[1, 1], A[1, 2] }, { A[2, 1], A[2, 2] } }), -Det2x2(new float[2,2]{ { A[0, 1], A[0, 2] }, { A[2, 1], A[2, 2] } }), Det2x2(new float[2,2]{ { A[0, 1], A[0, 2] }, { A[1, 1], A[1, 2] } }) },
            { -Det2x2(new float[2,2]{ { A[1, 0], A[1, 2] }, { A[2, 0], A[2, 2] } }), Det2x2(new float[2,2]{ { A[0, 0], A[0, 2] }, { A[2, 0], A[2, 2] } }), -Det2x2(new float[2,2]{ { A[0, 0], A[0, 2] }, { A[1, 0], A[1, 2] } }) },
            { Det2x2(new float[2,2]{ { A[1, 0], A[1, 1] }, { A[2, 0], A[2, 1] } }), -Det2x2(new float[2,2]{ { A[0, 0], A[0, 1] }, { A[2, 0], A[2, 1] } }), Det2x2(new float[2,2]{ { A[0, 0], A[0, 1] }, { A[1, 0], A[1, 1] } }) }
            };
        }

        public static float[,] MultInverse(float[,] A)
        {
            float detA = Det3x3(A);
            if (detA == 0) return null;
            return Multiply3x3(Cofactor(A), 1f / detA);
        }

        public static void Print3x3(float[,] A)
        {
            for (int i = 0; i < 3; i++)
            {
                Debug.Log("[" + A[i, 0] + ", " + A[i, 1] + ", " + A[i, 2] + "]");
            }
        }

        public static double[,] Matmul(double[,] A, double[,] B)
        {
            double[,] result = new double[3, 3] { { 0f, 0f, 0f }, { 0f, 0f, 0f }, { 0f, 0f, 0f } };
            double sum = 0f;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sum = 0f;
                    for (int k = 0; k < 3; k++)
                        sum = sum + A[i, k] * B[k, j];
                    result[i, j] = sum;
                }
            }
            return result;
        }

        public static double[] Matmul(double[,] A, double[] B)
        {
            double[] result = new double[3];
            double sum = 0f;
            for (int i = 0; i < 3; i++)
            {
                sum = 0f;
                for (int k = 0; k < 3; k++)
                    sum = sum + A[i, k] * B[k];
                result[i] = sum;
            }
            return result;
        }

        public static double[,] Add3x3(double[,] A, double[,] B)
        {
            double[,] result = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result[i, j] = A[i, j] + B[i, j];
            return result;
        }
        public static double[,] Add3x3(double[,] A, double b)
        {
            double[,] result = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result[i, j] = A[i, j] + b;
            return result;
        }
        public static double[,] Multiply3x3(double[,] A, double b)
        {
            double[,] result = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result[i, j] = A[i, j] * b;
            return result;
        }

        public static double Det3x3(double[,] A)
        {
            return A[0, 0] * A[1, 1] * A[2, 2] + A[0, 1] * A[1, 2] * A[2, 0] + A[0, 2] * A[1, 0] * A[2, 1] -
                   A[0, 0] * A[1, 2] * A[2, 1] - A[0, 1] * A[1, 0] * A[2, 2] - A[0, 2] * A[1, 1] * A[2, 0];
        }
        public static double Det2x2(double[,] A)
        {
            return A[0, 0] * A[1, 1] - A[0, 1] * A[1, 0];
        }

        public static double[,] Cofactor(double[,] A)
        {
            return new double[3, 3]
            {
            { Det2x2(new double[2,2]{ { A[1, 1], A[1, 2] }, { A[2, 1], A[2, 2] } }), -Det2x2(new double[2,2]{ { A[0, 1], A[0, 2] }, { A[2, 1], A[2, 2] } }), Det2x2(new double[2,2]{ { A[0, 1], A[0, 2] }, { A[1, 1], A[1, 2] } }) },
            { -Det2x2(new double[2,2]{ { A[1, 0], A[1, 2] }, { A[2, 0], A[2, 2] } }), Det2x2(new double[2,2]{ { A[0, 0], A[0, 2] }, { A[2, 0], A[2, 2] } }), -Det2x2(new double[2,2]{ { A[0, 0], A[0, 2] }, { A[1, 0], A[1, 2] } }) },
            { Det2x2(new double[2,2]{ { A[1, 0], A[1, 1] }, { A[2, 0], A[2, 1] } }), -Det2x2(new double[2,2]{ { A[0, 0], A[0, 1] }, { A[2, 0], A[2, 1] } }), Det2x2(new double[2,2]{ { A[0, 0], A[0, 1] }, { A[1, 0], A[1, 1] } }) }
            };
        }

        public static double[,] MultInverse(double[,] A)
        {
            double detA = Det3x3(A);
            if (detA == 0) return null;
            return Multiply3x3(Cofactor(A), 1f / detA);
        }

        public static void Print3x3(double[,] A)
        {
            for (int i = 0; i < 3; i++)
            {
                Debug.Log("[" + A[i, 0] + ", " + A[i, 1] + ", " + A[i, 2] + "]");
            }
        }
        #endregion

        #region float3
        public static float[] Add3(float[] A, float[] B)
        {
            var value = new float[3];
            value[0] = A[0] + B[0];
            value[1] = A[1] + B[1];
            value[2] = A[2] + B[2];
            return value;
        }
        public static float[] Add3(float[] A, float b)
        {
            var value = new float[3];
            value[0] = A[0] + b;
            value[1] = A[1] + b;
            value[2] = A[2] + b;
            return value;
        }
        public static float[] Sub3(float[] A, float[] B)
        {
            var value = new float[3];
            value[0] = A[0] - B[0];
            value[1] = A[1] - B[1];
            value[2] = A[2] - B[2];
            return value;
        }
        public static float[] Sub3(float[] A, float b)
        {
            var value = new float[3];
            value[0] = A[0] - b;
            value[1] = A[1] - b;
            value[2] = A[2] - b;
            return value;
        }
        public static float[] Mult3(float[] A, float b)
        {
            var value = new float[3];
            value[0] = A[0] * b;
            value[1] = A[1] * b;
            value[2] = A[2] * b;
            return value;
        }
        public static float[] Div3(float[] A, float b)
        {
            var value = new float[3];
            value[0] = A[0] / b;
            value[1] = A[1] / b;
            value[2] = A[2] / b;
            return value;
        }

        public static double[] Add3(double[] A, double[] B)
        {
            var value = new double[3];
            value[0] = A[0] + B[0];
            value[1] = A[1] + B[1];
            value[2] = A[2] + B[2];
            return value;
        }
        public static double[] Add3(double[] A, double b)
        {
            var value = new double[3];
            value[0] = A[0] + b;
            value[1] = A[1] + b;
            value[2] = A[2] + b;
            return value;
        }
        public static double[] Sub3(double[] A, double[] B)
        {
            var value = new double[3];
            value[0] = A[0] - B[0];
            value[1] = A[1] - B[1];
            value[2] = A[2] - B[2];
            return value;
        }
        public static double[] Sub3(double[] A, double b)
        {
            var value = new double[3];
            value[0] = A[0] - b;
            value[1] = A[1] - b;
            value[2] = A[2] - b;
            return value;
        }
        public static double[] Mult3(double[] A, double b)
        {
            var value = new double[3];
            value[0] = A[0] * b;
            value[1] = A[1] * b;
            value[2] = A[2] * b;
            return value;
        }
        public static double[] Div3(double[] A, double b)
        {
            var value = new double[3];
            value[0] = A[0] / b;
            value[1] = A[1] / b;
            value[2] = A[2] / b;
            return value;
        }

        #endregion

        public struct SystemMatrices
        {
            public float[,] M;
            public float[,] N;
        }
    }
}