using System.Text;
using UnityEngine;

namespace Utility
{
    public static class Util
    {
        /// 받아온 값이 min, max의 범위에 벗어나면 반대 범위 값을 반환하는 함수. 
        public static int ClampIndex(int index, int min, int max)
        {
            var ret = index;

            if (index < min)
                ret = max;

            if (index > max)
                ret = min;

            return ret;
        }

        /// GameObject를 생성하는 함수.
        public static GameObject CreateObject(string gameObjectName, Transform parent, Vector3 position = new Vector3())
        {
            var go = new GameObject(gameObjectName);
            
            go.transform.SetParent(parent);
            go.transform.position = position;
            return go;
        }

        /// GameObject를 생성하고 T 컴포넌트를 추가하는 함수.
        public static T CreateObject<T>(string gameObjectName, Transform parent, Vector3 position = new Vector3()) where T : Component
        {
            var go = new GameObject(gameObjectName);
            var ret = go.AddComponent<T>();
            
            go.transform.SetParent(parent);
            go.transform.position = position;
            return ret;
        }

        /// 조건에 따라 True = 1을, False = -1을 반환하는 함수.
        public static int GetToggleOne(bool isCondition)
        {
            return isCondition ? 1 : -1;
        }
        
        /// 랜덤으로 위치를 반환하는 함수.
        public static Vector2 GetRandomDirVec(Vector2 curPos, float rangeX, float rangeY)
        {
            var seed = Random.Range(0, int.MaxValue);
            var toX = Random.Range(-rangeX, rangeX) + curPos.x;
            
            Random.InitState(seed);
            
            var toY = Random.Range(-rangeY, rangeY) + curPos.y;
            var toPos = new Vector2(toX, toY);
            
            return toPos;
        }
        
        /// 여러 문자열을 합쳐야하는 경우 사용할 함수.
        public static string AppendString(string separate = "", string endPrefix = "", params string[] values)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < values.Length; i++)
            {
                builder.Append(values[i]);

                // 구분 문자없다면 넘어감.
                if (string.IsNullOrEmpty(separate))
                    continue;

                // 마지막 순서라면 넘어감.
                if (i >= values.Length - 1)
                    continue;
                
                builder.Append(separate);
            }

            // 파일의 확장자명처럼 마지막에 구분 문자없이 합쳐야하는 경우.
            if (string.IsNullOrEmpty(endPrefix) is false)
                builder.Append(endPrefix);

            return builder.ToString();
        }
        
        /// 두 거리를 받아 비교하여 일정 거리만큼 도달했는지 여부를 반환.
        public static bool CheckDistance(Vector2 fromPos, Vector2 toPos, float distance)
        {
            return Vector2.Distance(fromPos, toPos) < distance;
        }
        
        /// 받아온 각도를 방향 벡터로 반환.
        public static Vector2 GetVectorFromAngle(float angle)
        {
            // 각도를 라디안으로 변환.
            var radian = angle * Mathf.Deg2Rad;
        
            // 각도를 기준으로 방향 벡터 계산.
            var x = Mathf.Cos(radian);
            var y = Mathf.Sin(radian);
        
            return new Vector2(x, y);
        }
    }
}