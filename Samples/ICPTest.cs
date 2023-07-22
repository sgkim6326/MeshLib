using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace MeshLib.Example
{
    public class ICPTest : MonoBehaviour
    {
        public Transform Source;
        public Transform Target;
        public Transform TestObject;
        IEnumerator Start()
        {
            Vector3[] MatrixSource = Source.GetComponentsInChildren<Transform>().Select(trans => trans.position).Skip(1).ToArray();
            Vector3[] MatrixTarget = Target.GetComponentsInChildren<Transform>().Select(trans => trans.position).Skip(1).ToArray();
            IRegistrationSolver cal = new Registration.ICPSolver(MatrixSource, MatrixTarget, 1000, 0.0001);
            Task task = Task.Run(async () =>
            {
                try
                {
                    await cal.AsyncComputeRegistration();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            yield return new WaitUntil(() => task.IsCompleted);
            cal.TranslateAndRotate(TestObject);
        }
    }
}