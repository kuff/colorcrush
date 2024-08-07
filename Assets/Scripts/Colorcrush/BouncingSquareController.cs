// Copyright (C) 2024 Peter Guld Leth

#region

using UnityEngine;

#endregion

namespace Colorcrush
{
    public class BouncingSquareController : MonoBehaviour
    {
        public float initialSpeed = 5f;
        public float speedDecayFactor = 0.7f; // Factor by which speed is reduced each bounce

        private float _currentSpeed;
        private Vector3 _direction;
        private Vector3 _dragOffset;
        private Vector3 _dragStartPos;
        private float _halfHeight;
        private float _halfWidth;

        private bool _isDragging;
        private Vector3 _lastMousePosition;
        private Camera _mainCamera;

        private void Start()
        {
            InitializeSquare();
        }

        private void Update()
        {
            if (_isDragging)
            {
                DragSquare();
            }
            else
            {
                MoveSquare();
            }

            CheckScreenBounds();
        }

        private void OnMouseDown()
        {
            BeginDrag();
        }

        private void OnMouseDrag()
        {
            if (Input.mousePresent)
            {
                var inputPos = Input.mousePosition;
                _lastMousePosition = _mainCamera.ScreenToWorldPoint(inputPos);
                DragSquare();
            }
        }

        private void OnMouseUp()
        {
            EndDrag();
        }

        private void OnMouseUpAsButton()
        {
            if (Input.mousePresent)
            {
                EndDrag();
            }
        }

        private void InitializeSquare()
        {
            // Initialize direction to a random direction
            _direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            _mainCamera = Camera.main;

            // Calculate the half width and height of the square based on its scale
            _halfWidth = transform.localScale.x / 2;
            _halfHeight = transform.localScale.y / 2;

            // Set initial speed
            _currentSpeed = initialSpeed;
        }

        private void MoveSquare()
        {
            // Move the square
            transform.Translate(_direction * (_currentSpeed * Time.deltaTime));
        }

        private void DragSquare()
        {
            Vector3 inputPos = Input.mousePresent ? Input.mousePosition : Input.GetTouch(0).position;
            var worldPos = _mainCamera.ScreenToWorldPoint(inputPos);
            transform.position =
                new Vector3(worldPos.x - _dragOffset.x, worldPos.y - _dragOffset.y, transform.position.z);
        }

        private void CheckScreenBounds()
        {
            var pos = transform.position;

            // Get the screen bounds in world coordinates
            var screenBottomLeft = _mainCamera.ViewportToWorldPoint(new Vector3(0, 0, _mainCamera.nearClipPlane));
            var screenTopRight = _mainCamera.ViewportToWorldPoint(new Vector3(1, 1, _mainCamera.nearClipPlane));

            // Bounce off the left and right edges
            if (pos.x - _halfWidth < screenBottomLeft.x || pos.x + _halfWidth > screenTopRight.x)
            {
                _direction.x = -_direction.x;
                // Correct the position to ensure it stays within bounds
                pos.x = Mathf.Clamp(pos.x, screenBottomLeft.x + _halfWidth, screenTopRight.x - _halfWidth);
                ReduceSpeed();
            }

            // Bounce off the top and bottom edges
            if (pos.y - _halfHeight < screenBottomLeft.y || pos.y + _halfHeight > screenTopRight.y)
            {
                _direction.y = -_direction.y;
                // Correct the position to ensure it stays within bounds
                pos.y = Mathf.Clamp(pos.y, screenBottomLeft.y + _halfHeight, screenTopRight.y - _halfHeight);
                ReduceSpeed();
            }

            transform.position = pos;
        }

        private void ReduceSpeed()
        {
            _currentSpeed *= speedDecayFactor;
            if (_currentSpeed < initialSpeed)
            {
                _currentSpeed = initialSpeed;
            }
        }

        private void BeginDrag()
        {
            _isDragging = true;
            _dragStartPos = transform.position;
            Vector3 inputPos = Input.mousePresent ? Input.mousePosition : Input.GetTouch(0).position;
            var worldPos = _mainCamera.ScreenToWorldPoint(inputPos);
            _dragOffset = worldPos - _dragStartPos;
        }

        private void EndDrag()
        {
            _isDragging = false;

            // Calculate the velocity based on the last mouse position
            Vector3 inputPos = Input.mousePresent ? Input.mousePosition : Input.GetTouch(0).position;
            var worldPos = _mainCamera.ScreenToWorldPoint(inputPos);
            var releaseVelocity = (worldPos - _lastMousePosition) / Time.deltaTime;

            // Set the direction and speed based on the release velocity
            _direction = releaseVelocity.normalized;
            _currentSpeed = releaseVelocity.magnitude;
            if (_currentSpeed < initialSpeed)
            {
                _currentSpeed = initialSpeed;
            }
        }

        private void OnTouchDrag()
        {
            if (Input.touchCount > 0)
            {
                Vector3 inputPos = Input.GetTouch(0).position;
                _lastMousePosition = _mainCamera.ScreenToWorldPoint(inputPos);
                DragSquare();
            }
        }

        private void OnTouchEnd()
        {
            if (Input.touchCount > 0)
            {
                EndDrag();
            }
        }
    }
}