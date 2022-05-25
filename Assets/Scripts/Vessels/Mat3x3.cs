using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Mat3x3
{

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

    public static Vector3 Matmul(float[,] A, Vector3 B)
    {
        Vector3 result = Vector3.zero;
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
        return Multiply3x3(Cofactor(A), 1f/detA);
    }

    public static void Print3x3(float[,] A)
    {
        for (int i = 0; i < 3; i++)
        {
            Debug.Log("[" + A[i, 0] + ", " + A[i, 1] + ", " + A[i, 2] + "]");
        }
    }
    #endregion

    public struct SystemMatrices
    {
        public float[,] M;
        public float[,] N;
    }
}
