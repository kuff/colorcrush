using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CalculateStage2coordinates : MonoBehaviour
{
    List<Vector3> stage2Coordinates; 

    List<(Vector3, bool)> direction0 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same
    List<(Vector3, bool)> direction1 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same
    List<(Vector3, bool)> direction2 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same
    List<(Vector3, bool)> direction3 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same
    List<(Vector3, bool)> direction4 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same
    List<(Vector3, bool)> direction5 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same
    List<(Vector3, bool)> direction6 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same
    List<(Vector3, bool)> direction7 = new List<(Vector3, bool)>();//TRUE=Selected/different, False=not-selected/same

    float circleExpansion = 0.0013f;

    
    public List<Vector3> Stage2Coordinates(List<Vector3> selected, List<Vector3> unSelected, Vector3 baseColor)//change to whatever is provided
    {
        
        //convert to what makes sense
        foreach (Vector3 currentCoordinate in selected)
        {
            //Vector3 currentCoordinate = selectedCircle.GetComponent<data>().xyYCoordinate;
            if (currentCoordinate != baseColor)
            {
                float angle = Mathf.Atan2(baseColor[1] - currentCoordinate[1], baseColor[0] - currentCoordinate[0]);
                switch (angle)
                {
                    case >3.1f and <3.2f:
                        direction0.Add((currentCoordinate, true));
                        break;
                    case > -2.4f and < -2.3f:
                        direction1.Add((currentCoordinate, true));
                        break;
                    case > -1.6f and < -1.5f:
                        direction2.Add((currentCoordinate, true));
                        break;
                    case > -0.8f and < -0.7f:
                        direction3.Add((currentCoordinate, true));
                        break;
                    case > -0.1f and < 0.1f:
                        direction4.Add((currentCoordinate, true));
                        break;
                    case < 2.4f and > 2.3f:
                        direction5.Add((currentCoordinate, true));
                        break;
                    case < 1.6f and > 1.5f:
                        direction6.Add((currentCoordinate, true));
                        break;
                    case < 0.8f and > 0.7f:
                        direction7.Add((currentCoordinate, true));
                        break;
                    default:
                        break;
                }

            
            }
        }
        foreach (Vector3 currentCoordinate in unSelected)
        {
            //Vector3 currentCoordinate = unSelectedCircle.GetComponent<data>().xyYCoordinate;
            if (currentCoordinate != baseColor)
            {
                float angle = Mathf.Atan2(baseColor[1] - currentCoordinate[1], baseColor[0] - currentCoordinate[0]);
                switch (angle)
                {
                    case > 3.1f and < 3.2f:
                        direction0.Add((currentCoordinate, false));
                        break;
                    case > -2.4f and < -2.3f:
                        direction1.Add((currentCoordinate, false));
                        break;
                    case > -1.6f and < -1.5f:
                        direction2.Add((currentCoordinate, false));
                        break;
                    case > -0.8f and < -0.7f:
                        direction3.Add((currentCoordinate, false));
                        break;
                    case > -0.1f and < 0.1f:
                        direction4.Add((currentCoordinate, false));
                        break;
                    case < 2.4f and > 2.3f:
                        direction5.Add((currentCoordinate, false));
                        break;
                    case < 1.6f and > 1.5f:
                        direction6.Add((currentCoordinate, false));
                        break;
                    case < 0.8f and > 0.7f:
                        direction7.Add((currentCoordinate, false));
                        break;
                    default:
                        break;
                }
            }
        }


        Debug.Log("before calculations");
        Debug.Log(direction0);
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction0, baseColor));
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction1, baseColor));
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction2, baseColor));
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction3, baseColor));
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction4, baseColor));
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction5, baseColor));
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction6, baseColor));
        stage2Coordinates.AddRange(calculateStage2CoordinatesForDirection(direction7, baseColor));
        

        return stage2Coordinates;
    }

    List<Vector3> calculateStage2CoordinatesForDirection(List<(Vector3, bool)> direction, Vector3 basexyY)
    {
        Vector3 nullVector = new Vector3();
        Vector3 startCoordinate = new Vector3();
        Vector3 endCoordinate = new Vector3();

        List<Vector3> returnCoordinates = new List<Vector3>();
        // Sort the list based on the distance between each Vector3 and basexyY
        List<(Vector3, bool)> ordered = direction.OrderBy(item => Vector3.Distance(item.Item1, basexyY)).ToList();

        //Get furstest unselected pair
        for(int i=0 ; i < ordered.Count; i += 2)
        {
            if(ordered[i].Item2 || ordered[i + 1].Item2)
            {
                if (startCoordinate == nullVector)
                {
                    startCoordinate = ordered[i].Item1;
                    Debug.Log(startCoordinate * 100);
                }
            }
            if(!ordered[i].Item2 || !ordered[i + 1].Item2)
            {
                endCoordinate = ordered[i].Item1;
            }

            /*
            if ((!ordered[i].Item2 && ordered[i + 1].Item2) || (ordered[i].Item2 && !ordered[i + 1].Item2))
            {
                if (startCoordinate == nullVector)
                {
                    startCoordinate = ordered[i].Item1;
                    Debug.Log(startCoordinate * 100);
                }
                endCoordinate = ordered[i].Item1;
            }
            */
        }
        if (startCoordinate == nullVector)//case 2 on paper
        {
            Debug.Log("startCoordinate is nullvector");
            return returnCoordinates;
            
        }
        if (endCoordinate == nullVector)//case 3 on paper
        {
            Debug.Log("endCoordinate is nullvector");
            return returnCoordinates;

        }
        
        Debug.Log("Start");
        Debug.Log(startCoordinate * 100);
        Debug.Log("end");
        Debug.Log(endCoordinate * 100);
        /*
        float distance = Vector3.Distance(endCoordinate, startCoordinate);

        float halfExpansion = circleExpansion / 2;
        float stepsf = distance / halfExpansion;
        int steps = (int) Mathf.RoundToInt(stepsf);
        Debug.Log("stepsf");
        Debug.Log(stepsf);
        Debug.Log("steps");
        Debug.Log(steps);
        */


        Vector3 unitVector = Vector3.Normalize((startCoordinate - basexyY));
        Vector3 stepVector = unitVector * (circleExpansion / 2);//im stuck
        //
        Vector3 point1 = startCoordinate + (stepVector*-1);
        Vector3 point4 = endCoordinate + stepVector;
        if(point1== point4)//case 1 on paper
        {
            Debug.Log("perfect result");
            return returnCoordinates;
        }
        Vector3 point2 = Vector3.Lerp(point1, point4, (1f / 3f));
        Vector3 point3 = Vector3.Lerp(point1, point4, (2f / 3f));
        //
        /*
        for (int step = -1; step <= steps + 1; step++)
        {

            returnCoordinates.Add(startCoordinate + stepVector * step);
        }
        */
        returnCoordinates.Add(point1);
        returnCoordinates.Add(point2);
        returnCoordinates.Add(point3);
        returnCoordinates.Add(point4);

        return returnCoordinates;
    }
}
