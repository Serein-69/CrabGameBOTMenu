using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SteamworksNative;

namespace BOTMenuMod
{

    public static class AutoPlayBlockDrop
    {
        public static bool enabled = false;

        private const int MAP_WIDTH = 64;
        private const int COLUMN_COUNT_PER_ROW = 4;
        private const float COLUMN_WIDTH = 16f;
        private const int TOTAL_COLUMNS = 16;

        private static Dictionary<int, bool> columnStatus = new Dictionary<int, bool>();
        private static Dictionary<int, int> blocksPerColumn = new Dictionary<int, int>();
        private static List<int> processedBlocks = new List<int>();

        private static float elapsedUpdate = 0f;
        private static Vector3 safePosition = Vector3.zero;
        private static float objectScanTimer = 0f;
        private static readonly List<GameObject> cachedSceneObjects = new List<GameObject>();

        public static void Update()
        {
            if (!enabled) return;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            bool isBlockDrop = UnityEngine.Object.FindObjectOfType<GameModeBlockDrop>() != null;
            if (!isBlockDrop)
            {
                processedBlocks.Clear();
                return;
            }

            Vector3 playerPos = myMovement.transform.position;
            elapsedUpdate += Time.deltaTime;
            objectScanTimer -= Time.deltaTime;

            if (elapsedUpdate > 0.1f)
            {
                elapsedUpdate = 0f;
                UpdateColumnStatus();

                int currentColumn = DetermineColumnIndex(playerPos);
                if (currentColumn >= 0 && currentColumn < TOTAL_COLUMNS)
                {
                    int currentHeight = columnStatus[currentColumn] == false ? blocksPerColumn[currentColumn] - 1 : blocksPerColumn[currentColumn];
                    safePosition = FindSafeNeighbour(currentColumn, currentHeight, playerPos);
                }
            }

            if (safePosition != Vector3.zero)
            {
                float distanceFromSafePos = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(safePosition.x, 0, safePosition.z));

                if (distanceFromSafePos > 1.5f)
                {
                    MoveWithManhattanBunnyHop(myMovement, safePosition);
                }
                else
                {
                    var rb = myMovement.GetComponent<Rigidbody>();
                    if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);

                    var target = FindClosestEnemy(myId, myMovement);
                    if (target != null)
                    {
                        float distToEnemy = Vector3.Distance(myMovement.transform.position, target.transform.position);

                        if (distToEnemy < 6f && Time.frameCount % 15 == 0)
                        {
                            ulong targetId = target.steamProfile.m_SteamID;
                            Vector3 pushDir = (target.transform.position - myMovement.transform.position).normalized;
                            pushDir.y = 0.8f;

                            try
                            {
                                MonoBehaviourPublicInpabyInInInUnique.PunchPlayer(targetId, pushDir * 3.5f);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        private static int DetermineColumnIndex(Vector3 position)
        {
            int col = Mathf.FloorToInt((position.x + (MAP_WIDTH / 2f)) / COLUMN_WIDTH);
            int row = Mathf.FloorToInt((position.z + (MAP_WIDTH / 2f)) / COLUMN_WIDTH);
            col = Mathf.Clamp(col, 0, COLUMN_COUNT_PER_ROW - 1);
            row = Mathf.Clamp(row, 0, COLUMN_COUNT_PER_ROW - 1);
            return col + row * COLUMN_COUNT_PER_ROW;
        }

        private static Vector3 CalculateColumnCenter(int columnIndex, Vector3 playerPos)
        {
            int column = columnIndex % COLUMN_COUNT_PER_ROW;
            int row = columnIndex / COLUMN_COUNT_PER_ROW;
            float x = (column * COLUMN_WIDTH) - ((MAP_WIDTH / 2f) - (COLUMN_WIDTH / 2f));
            float z = (row * COLUMN_WIDTH) - ((MAP_WIDTH / 2f) - (COLUMN_WIDTH / 2f));
            return new Vector3(x, playerPos.y, z);
        }

        private static void UpdateColumnStatus()
        {
            for (int i = 0; i < TOTAL_COLUMNS; i++)
            {
                columnStatus[i] = true;
                if (!blocksPerColumn.ContainsKey(i)) blocksPerColumn[i] = 0;
            }

            if (objectScanTimer <= 0f)
            {
                objectScanTimer = 1f;
                cachedSceneObjects.Clear();
                var found = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var o in found)
                {
                    if (o != null) cachedSceneObjects.Add(o);
                }
            }

            foreach (var obj in cachedSceneObjects)
            {
                if (obj == null) continue;
                if (obj.name == "FallBlock(Clone)")
                {
                    int blockID = obj.GetInstanceID();
                    int columnIndex = DetermineColumnIndex(obj.transform.position);

                    if (columnIndex >= 0 && columnIndex < TOTAL_COLUMNS)
                    {
                        columnStatus[columnIndex] = false;

                        if (!processedBlocks.Contains(blockID))
                        {
                            processedBlocks.Add(blockID);
                            blocksPerColumn[columnIndex]++;
                        }
                    }
                }
            }
        }

        private static Vector3 FindSafeNeighbour(int currentColumn, int currentHeight, Vector3 playerPos)
        {
            List<int> higherColumns = new List<int>();
            List<int> sameLevelColumns = new List<int>();
            List<int> lowerColumns = new List<int>();

            int[] NEIGHBOUR_OFFSETS = { 0, -1, 1, -4, 4, -5, 5, -3, 3 };

            foreach (int offset in NEIGHBOUR_OFFSETS)
            {
                int neighbourColumn = currentColumn + offset;

                if (neighbourColumn < 0 || neighbourColumn >= TOTAL_COLUMNS) continue;
                if (Mathf.Abs((currentColumn % COLUMN_COUNT_PER_ROW) - (neighbourColumn % COLUMN_COUNT_PER_ROW)) > 1) continue;

                if (columnStatus.ContainsKey(neighbourColumn) && columnStatus[neighbourColumn])
                {
                    int neighbourHeight = blocksPerColumn[neighbourColumn];

                    if (!IsBlockedByWall(currentColumn, neighbourColumn, currentHeight))
                    {
                        if (neighbourHeight == currentHeight + 1) higherColumns.Add(neighbourColumn);
                        else if (neighbourHeight == currentHeight) sameLevelColumns.Add(neighbourColumn);
                        else if (neighbourHeight < currentHeight) lowerColumns.Add(neighbourColumn);
                    }
                }
            }

            int targetColumn = currentColumn;
            int[] CENTRAL_COLUMNS = { 5, 6, 9, 10 };

            if (higherColumns.Count > 0)
                targetColumn = higherColumns.OrderBy(c => CENTRAL_COLUMNS.Min(center => Mathf.Abs(c - center))).First();
            else if (sameLevelColumns.Count > 0)
                targetColumn = sameLevelColumns.OrderBy(c => CENTRAL_COLUMNS.Min(center => Mathf.Abs(c - center))).First();
            else if (lowerColumns.Count > 0)
                targetColumn = lowerColumns.OrderBy(c => CENTRAL_COLUMNS.Min(center => Mathf.Abs(c - center))).First();

            return CalculateColumnCenter(targetColumn, playerPos);
        }

        private static bool IsBlockedByWall(int fromColumn, int toColumn, int currentHeight)
        {
            int diff = Mathf.Abs(fromColumn - toColumn);
            if (diff == 5 || diff == 3)
            {
                int colDiff = (toColumn % COLUMN_COUNT_PER_ROW) - (fromColumn % COLUMN_COUNT_PER_ROW);
                int rowDiff = (toColumn / COLUMN_COUNT_PER_ROW) - (fromColumn / COLUMN_COUNT_PER_ROW);

                int adj1 = fromColumn + colDiff;
                int adj2 = fromColumn + (rowDiff * COLUMN_COUNT_PER_ROW);

                if (blocksPerColumn.ContainsKey(adj1) && blocksPerColumn.ContainsKey(adj2))
                {
                    if (blocksPerColumn[adj1] > currentHeight && blocksPerColumn[adj2] > currentHeight) return true;
                }
            }
            return false;
        }

        private static void MoveWithManhattanBunnyHop(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 targetPos)
        {
            Vector3 diff = targetPos - player.transform.position;
            diff.y = 0;
            Vector3 moveDir = Vector3.zero;

            if (Mathf.Abs(diff.x) > 0.5f) moveDir = new Vector3(Mathf.Sign(diff.x), 0, 0);
            else if (Mathf.Abs(diff.z) > 0.5f) moveDir = new Vector3(0, 0, Mathf.Sign(diff.z));
            else moveDir = diff.normalized;

            float currentSpeed = 16f;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = new Vector3(moveDir.x * currentSpeed, rb.velocity.y, moveDir.z * currentSpeed);

                if (player.field_Private_Boolean_3)
                {
                    player.Jump();
                }
            }
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique FindClosestEnemy(ulong myId, MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique myMovement)
        {
            MonoBehaviourPublicCSstReshTrheObplBojuUnique closestEnemy = null;
            float minDist = float.MaxValue;

            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var enemy = kvp.Value;
                if (enemy == null || enemy.dead || enemy.transform == null) continue;

                float dist = Vector3.Distance(myMovement.transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestEnemy = enemy;
                }
            }
            return closestEnemy;
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlaySnowBrawl
    {
        public static bool enabled = false;
        public static float runSpeed = 14f;

        private static float _dangerDistance = 15f;
        private static Vector3 _currentDodgeDir = Vector3.zero;
        private static float _dodgeCooldown = 0f;
        private static float _waypointTimer = 0f;
        private static Vector3 _waypointDir = Vector3.zero;
        private static float _pileScanTimer = 0f;
        private static readonly List<GameObject> _cachedPiles = new List<GameObject>();

        public static void Update()
        {
            if (!enabled) return;
            WaterWalkManager.enabled = true;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            bool isSnowBrawl = UnityEngine.Object.FindObjectOfType<GameModeSnowBrawl>() != null;
            if (!isSnowBrawl) return;

            _dodgeCooldown -= Time.deltaTime;
            _waypointTimer -= Time.deltaTime;
            _pileScanTimer -= Time.deltaTime;
            Vector3 myPos = myMovement.transform.position;

            GameObject mostDangerousSnowball = null;
            float highestDangerScore = 0f;

            var allRbs = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
            foreach (var rb in allRbs)
            {
                if (rb == null || rb.gameObject == null) continue;
                if (rb.gameObject.name.Contains("Snowball") && rb.gameObject.name.Contains("Clone"))
                {
                    if (rb.velocity.magnitude < 2f) continue;
                    Vector3 toMe = myPos - rb.transform.position;
                    float distance = toMe.magnitude;

                    if (distance < _dangerDistance)
                    {
                        float dot = Vector3.Dot(rb.velocity.normalized, toMe.normalized);
                        if (dot > 0.8f)
                        {
                            float score = (rb.velocity.magnitude / distance) * dot;
                            if (score > highestDangerScore)
                            {
                                highestDangerScore = score;
                                mostDangerousSnowball = rb.gameObject;
                            }
                        }
                    }
                }
            }

            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            bool hasSnowball = inv != null && inv.currentItem != null && inv.currentItem.name.Contains("Snowball");

            if (mostDangerousSnowball != null)
            {
                var rb = mostDangerousSnowball.GetComponent<Rigidbody>();
                if (_dodgeCooldown <= 0)
                {
                    Vector3 incomingDir = rb.velocity.normalized;
                    incomingDir.y = 0;
                    Vector3 leftDodge = Vector3.Cross(incomingDir, Vector3.up).normalized;
                    _currentDodgeDir = UnityEngine.Random.value > 0.5f ? leftDodge : -leftDodge;
                    _dodgeCooldown = 0.5f;
                }
                ExecuteMovement(myMovement, _currentDodgeDir, true);
            }
            else if (hasSnowball)
            {
                var target = GetClosestVisibleEnemy(myId, myMovement);

                if (target != null)
                {
                    var rb = myMovement.GetComponent<Rigidbody>();
                    if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
                    inv.currentItem.transform.position = target.transform.position + new Vector3(0, 1.5f, 0);
                    inv.UseItem();
                }
                else
                {
                    HuntClosestEnemy(myId, myMovement);
                }
            }
            else
            {
                if (_dodgeCooldown <= 0) SearchAndRunToSnowballPile(myMovement, myPos);
            }
        }

        private static void HuntClosestEnemy(ulong myId, MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique myMovement)
        {
            MonoBehaviourPublicCSstReshTrheObplBojuUnique closestEnemy = null;
            float minDist = float.MaxValue;
            Vector3 myPos = myMovement.transform.position;

            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var enemy = kvp.Value;
                if (enemy == null || enemy.dead || enemy.transform == null) continue;

                float dist = Vector3.Distance(myPos, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null)
            {
                Vector3 moveDir = CalculateSmartPath(myPos, closestEnemy.transform.position);
                ExecuteMovement(myMovement, moveDir, false);
            }
        }

        private static void SearchAndRunToSnowballPile(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 myPos)
        {
            GameObject closestPile = null;
            float minDistance = float.MaxValue;

            if (_pileScanTimer <= 0f)
            {
                _pileScanTimer = 1f;
                _cachedPiles.Clear();
                var foundPiles = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var o in foundPiles)
                {
                    if (o != null && o.name.Contains("SnowballPile")) _cachedPiles.Add(o);
                }
            }

            foreach (var obj in _cachedPiles)
            {
                if (obj == null || !obj.activeInHierarchy) continue;

                if (obj.name.Contains("SnowballPile"))
                {
                    if (obj.transform.position.y - myPos.y > 2.5f) continue;
                    float dist = Vector3.Distance(myPos, obj.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPile = obj;
                    }
                }
            }

            if (closestPile != null)
            {
                if (minDistance > 2.5f)
                {
                    Vector3 moveDir = CalculateSmartPath(myPos, closestPile.transform.position);
                    ExecuteMovement(player, moveDir, false);
                }
                else
                {
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
                    var interact = closestPile.GetComponent<MonoBehaviour1PublicBoInSiUnique>();
                    if (interact != null) MonoBehaviourPublicInpabyInInInUnique.TryInteract(interact.id);
                }
            }
        }

        private static Vector3 CalculateSmartPath(Vector3 myPos, Vector3 targetPos)
        {
            Vector3 directDir = (targetPos - myPos).normalized;
            directDir.y = 0;

            if (_waypointTimer > 0) return _waypointDir;

            Vector3 checkPos = myPos + Vector3.up * 0.5f;
            if (Physics.Raycast(checkPos, directDir, out RaycastHit hit, 2.0f))
            {
                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                {
                    Vector3 hitNormal = hit.normal;
                    hitNormal.y = 0;
                    Vector3 slideDir = Vector3.Cross(hitNormal, Vector3.up).normalized;
                    float dot = Vector3.Dot(slideDir, directDir);
                    if (dot < 0) slideDir = -slideDir;

                    _waypointDir = slideDir;
                    _waypointTimer = 0.4f;
                    return _waypointDir;
                }
            }
            return directDir;
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetClosestVisibleEnemy(ulong myId, MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique myMovement)
        {
            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return null;
            MonoBehaviourPublicCSstReshTrheObplBojuUnique closestEnemy = null;
            float minDist = float.MaxValue;
            Vector3 myEyePos = Camera.main != null ? Camera.main.transform.position : myMovement.transform.position + Vector3.up * 1.5f;

            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var enemy = kvp.Value;
                if (enemy == null || enemy.dead || enemy.transform == null) continue;

                float dist = Vector3.Distance(myMovement.transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    Vector3 enemyChest = enemy.transform.position + Vector3.up * 1.0f;
                    if (!Physics.Linecast(myEyePos, enemyChest, out RaycastHit hit) || hit.collider.transform.root == enemy.transform.root)
                    {
                        minDist = dist;
                        closestEnemy = enemy;
                    }
                }
            }
            return closestEnemy;
        }

        private static void ExecuteMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 dir, bool isDodge)
        {
            float currentSpeed = isDodge ? 18f : runSpeed;
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = new Vector3(dir.x * currentSpeed, rb.velocity.y, dir.z * currentSpeed);
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlayDodgeball
    {
        public static bool enabled = false;
        public static float runSpeed = 14f;
        public static float dodgeSpeed = 18f;

        private static float _dangerDistance = 15f;
        private static float _dodgeCooldown = 0f;
        private static Vector3 _currentDodgeDir = Vector3.zero;
        private static int _targetIndex = 0;
        private static float _waypointTimer = 0f;
        private static Vector3 _waypointDir = Vector3.zero;
        private static float _pileScanTimer = 0f;
        private static readonly List<GameObject> _cachedPiles = new List<GameObject>();

        public static void Update()
        {
            if (!enabled) return;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            string mapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
            if (!mapName.Contains("dodgy")) return;

            _dodgeCooldown -= Time.deltaTime;
            _waypointTimer -= Time.deltaTime;
            _pileScanTimer -= Time.deltaTime;
            Vector3 myPos = myMovement.transform.position;

            GameObject mostDangerousSnowball = null;
            float highestDangerScore = 0f;

            var allRbs = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
            foreach (var rb in allRbs)
            {
                if (rb == null || rb.gameObject == null) continue;
                if (rb.gameObject.name.Contains("Snowball") && rb.gameObject.name.Contains("Clone"))
                {
                    if (rb.velocity.magnitude < 2f) continue;
                    Vector3 toMe = myPos - rb.transform.position;
                    float distance = toMe.magnitude;

                    if (distance < _dangerDistance)
                    {
                        float dot = Vector3.Dot(rb.velocity.normalized, toMe.normalized);
                        if (dot > 0.8f)
                        {
                            float score = (rb.velocity.magnitude / distance) * dot;
                            if (score > highestDangerScore)
                            {
                                highestDangerScore = score;
                                mostDangerousSnowball = rb.gameObject;
                            }
                        }
                    }
                }
            }

            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            bool hasSnowball = inv != null && inv.currentItem != null && inv.currentItem.name.Contains("Snowball");

            if (mostDangerousSnowball != null)
            {
                var rb = mostDangerousSnowball.GetComponent<Rigidbody>();
                if (_dodgeCooldown <= 0)
                {
                    Vector3 incomingDir = rb.velocity.normalized;
                    incomingDir.y = 0;
                    Vector3 leftDodge = Vector3.Cross(incomingDir, Vector3.up).normalized;
                    _currentDodgeDir = UnityEngine.Random.value > 0.5f ? leftDodge : -leftDodge;
                    _dodgeCooldown = 0.5f;
                }
                ExecuteMovement(myMovement, _currentDodgeDir, true);
            }
            else if (hasSnowball)
            {
                WanderAround(myMovement);
                var target = FindBestEnemy(myId, myPos);
                if (target != null)
                {
                    inv.currentItem.transform.position = target.transform.position + new Vector3(0, 1.5f, 0);
                    inv.UseItem();
                }
            }
            else
            {
                if (_dodgeCooldown <= 0) SearchAndRunToSnowballPile(myMovement, myPos);
            }
        }

        private static void SearchAndRunToSnowballPile(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 myPos)
        {
            GameObject closestPile = null;
            float minDistance = float.MaxValue;

            if (_pileScanTimer <= 0f)
            {
                _pileScanTimer = 1f;
                _cachedPiles.Clear();
                var foundPiles = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var o in foundPiles)
                {
                    if (o != null && o.name.Contains("SnowballPile")) _cachedPiles.Add(o);
                }
            }

            foreach (var obj in _cachedPiles)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                if (obj.name.Contains("SnowballPile"))
                {
                    float dist = Vector3.Distance(myPos, obj.transform.position);
                    if (!TeamRadarManager.IsEnemy(myPos, obj.transform.position) && dist < minDistance)
                    {
                        minDistance = dist;
                        closestPile = obj;
                    }
                }
            }

            if (closestPile != null)
            {
                if (minDistance > 2.5f)
                {
                    Vector3 moveDir = CalculateSmartPath(myPos, closestPile.transform.position);
                    ExecuteMovement(player, moveDir, false);
                }
                else
                {
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
                    var interact = closestPile.GetComponent<MonoBehaviour1PublicBoInSiUnique>();
                    if (interact != null) MonoBehaviourPublicInpabyInInInUnique.TryInteract(interact.id);
                }
            }
        }

        private static Vector3 CalculateSmartPath(Vector3 myPos, Vector3 targetPos)
        {
            Vector3 directDir = (targetPos - myPos).normalized;
            directDir.y = 0;

            if (_waypointTimer > 0) return _waypointDir;

            Vector3 checkPos = myPos + Vector3.up * 0.5f;
            if (Physics.Raycast(checkPos, directDir, out RaycastHit hit, 2.0f))
            {
                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                {
                    Vector3 hitNormal = hit.normal;
                    hitNormal.y = 0;
                    Vector3 slideDir = Vector3.Cross(hitNormal, Vector3.up).normalized;
                    float dot = Vector3.Dot(slideDir, directDir);
                    if (dot < 0) slideDir = -slideDir;

                    _waypointDir = slideDir;
                    _waypointTimer = 0.4f;
                    return _waypointDir;
                }
            }
            return directDir;
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique FindBestEnemy(ulong myId, Vector3 myPos)
        {
            var playersList = new List<MonoBehaviourPublicCSstReshTrheObplBojuUnique>();
            foreach (var p in GameManager.Instance.activePlayers.Values) playersList.Add(p);

            if (playersList.Count == 0) return null;
            if (_targetIndex >= playersList.Count) _targetIndex = 0;

            int attempts = 0;
            while (attempts < playersList.Count)
            {
                var target = playersList[_targetIndex];
                bool baseValid = (target != null && target.steamProfile.m_SteamID != myId && !target.dead);

                if (baseValid && TeamRadarManager.IsEnemy(myPos, target.transform.position))
                {
                    _targetIndex = (_targetIndex + 1) % playersList.Count;
                    return target;
                }
                _targetIndex = (_targetIndex + 1) % playersList.Count;
                attempts++;
            }
            return null;
        }

        private static void WanderAround(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player)
        {
            float t = Time.time;
            float x = Mathf.Sin(t * 1.5f) * 3f;
            float z = Mathf.Cos(t * 1.1f) * 3f;
            ExecuteMovement(player, new Vector3(x, 0, z).normalized, false);
        }

        private static void ExecuteMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 dir, bool isDodge)
        {
            float currentSpeed = isDodge ? dodgeSpeed : runSpeed;
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = new Vector3(dir.x * currentSpeed, rb.velocity.y, dir.z * currentSpeed);
            }
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlayTileDrive
    {
        public static bool enabled = false;
        public static float runSpeed = 20f;

        private static List<Vector3> _allTiles = new List<Vector3>();
        private static Vector3 _targetPos = Vector3.zero;
        private static float _unstuckModeTimer = 0f;
        private static Vector3 _unstuckDir = Vector3.zero;
        private static Vector3 _lastCheckPos = Vector3.zero;
        private static float _stuckTimer = 0f;
        private static float _checkTimer = 0f;

        public static void Update()
        {
            if (!enabled) return;

            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return;
            if (LobbyManager.Instance.gameMode.id != 9)
            {
                _allTiles.Clear();
                _targetPos = Vector3.zero;
                return;
            }

            HandleUnstuckSystem(myMovement);
            Vector3 myPos = myMovement.transform.position;

            if (_allTiles.Count == 0)
            {
                var allObjs = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjs)
                {
                    if (obj == null || !obj.activeInHierarchy) continue;
                    string name = obj.name.ToLower();
                    if (name.Contains("tile") && !name.Contains("drive") && !name.Contains("ui"))
                    {
                        _allTiles.Add(obj.transform.position);
                    }
                }
            }

            if (_allTiles.Count == 0) return;

            if (_targetPos == Vector3.zero)
            {
                var farTiles = _allTiles.Where(p => Vector3.Distance(myPos, p) > 10f).ToList();
                _targetPos = farTiles.Count > 0 ? farTiles[UnityEngine.Random.Range(0, farTiles.Count)] : _allTiles[UnityEngine.Random.Range(0, _allTiles.Count)];
            }

            if (_targetPos != Vector3.zero)
            {
                float dist = Vector3.Distance(myPos, _targetPos);
                if (dist > 1.5f)
                {
                    MoveOnFlatGround(myMovement, _targetPos);
                }
                else
                {
                    var rb = myMovement.GetComponent<Rigidbody>();
                    if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);

                    if (myMovement.field_Private_Boolean_3)
                    {
                        myMovement.Jump();
                        _targetPos = Vector3.zero;
                    }
                }
            }
        }

        private static void MoveOnFlatGround(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 targetPos)
        {
            Vector3 myPos = player.transform.position;
            Vector3 diff = targetPos - myPos;
            diff.y = 0;
            Vector3 moveDir = diff.normalized;

            Vector3 checkPos = myPos + Vector3.up * 0.5f;
            if (Physics.Raycast(checkPos, moveDir, out RaycastHit hit, 2f))
            {
                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                {
                    Vector3 hitNormal = hit.normal;
                    hitNormal.y = 0;
                    Vector3 slideDir = Vector3.Cross(hitNormal, Vector3.up).normalized;
                    if (Vector3.Dot(slideDir, moveDir) < 0) slideDir = -slideDir;
                    moveDir = slideDir;
                }
            }

            if (_unstuckModeTimer > 0) moveDir = _unstuckDir;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = new Vector3(moveDir.x * runSpeed, rb.velocity.y, moveDir.z * runSpeed);
            }
        }

        private static void HandleUnstuckSystem(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player)
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= 0.2f)
            {
                _checkTimer = 0f;
                if (Vector3.Distance(player.transform.position, _lastCheckPos) < 0.2f) _stuckTimer += 0.2f;
                else _stuckTimer = 0f;
                _lastCheckPos = player.transform.position;
            }

            if (_stuckTimer >= 0.6f)
            {
                _stuckTimer = 0f;
                _unstuckModeTimer = 0.6f;
                Vector3 forward = player.transform.forward;
                forward.y = 0;
                Vector3 sideDir = Vector3.Cross(forward, Vector3.up).normalized;
                _unstuckDir = (UnityEngine.Random.value > 0.5f ? sideDir : -sideDir);
                _targetPos = Vector3.zero;
            }
            if (_unstuckModeTimer > 0) _unstuckModeTimer -= Time.deltaTime;
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlayDeathFromAbove
    {
        public static bool enabled = false;
        public static float runSpeed = 20f;

        private const float MAP_WIDTH = 96f;
        private const int COLUMNS_PER_ROW = 12;
        private const float COLUMN_WIDTH = 8f;
        private const int TOTAL_COLUMNS = 144;

        private static bool[] _columnSafeStatus = new bool[TOTAL_COLUMNS];
        private static Vector3 _safePos = Vector3.zero;
        private static float _recheckTimer = 0f;
        private static float _unstuckModeTimer = 0f;
        private static Vector3 _unstuckDir = Vector3.zero;
        private static Vector3 _lastCheckPos = Vector3.zero;
        private static float _stuckTimer = 0f;
        private static float _checkTimer = 0f;

        public static void Update()
        {
            if (!enabled) return;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return;
            if (LobbyManager.Instance.gameMode.id != 14) return;

            HandleUnstuckSystem(myMovement);
            Vector3 myPos = myMovement.transform.position;

            _recheckTimer -= Time.deltaTime;
            if (_recheckTimer <= 0)
            {
                _recheckTimer = 0.1f;
                UpdateSunlightStatus(myPos.y);
                _safePos = FindSafePosition(myPos);
            }

            Vector3 targetDestination = _safePos != Vector3.zero ? _safePos : Vector3.zero;
            float dist = Vector3.Distance(new Vector3(myPos.x, 0, myPos.z), new Vector3(targetDestination.x, 0, targetDestination.z));

            if (dist > 0.5f)
            {
                Vector3 moveDir = (targetDestination - myPos).normalized;
                moveDir.y = 0;
                if (_unstuckModeTimer > 0) moveDir = _unstuckDir;
                ExecuteMovement(myMovement, moveDir, false);
            }
            else
            {
                var rb = myMovement.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);

                var target = FindClosestEnemy(myId, myMovement);
                if (target != null)
                {
                    float distToEnemy = Vector3.Distance(myMovement.transform.position, target.transform.position);
                    if (distToEnemy < 6f && Time.frameCount % 15 == 0)
                    {
                        ulong targetId = target.steamProfile.m_SteamID;
                        Vector3 pushDir = (target.transform.position - myMovement.transform.position).normalized;
                        pushDir.y = 0.8f;

                        try { MonoBehaviourPublicInpabyInInInUnique.PunchPlayer(targetId, pushDir * 3.5f); } catch { }
                    }
                }
            }
        }

        private static void UpdateSunlightStatus(float playerY)
        {
            int layerMask = LayerMask.GetMask("Object");
            for (int i = 0; i < TOTAL_COLUMNS; i++)
            {
                Vector3 center = CalculateColumnCenter(i);
                center.y = playerY + 0.5f;
                _columnSafeStatus[i] = layerMask != 0 ? !Physics.Raycast(center, Vector3.up, 500f, layerMask) : !Physics.Raycast(center, Vector3.up, 40f);
            }
        }

        private static Vector3 FindSafePosition(Vector3 playerPos)
        {
            int myCol = DetermineColumnIndex(playerPos);
            Vector3 myCenter = CalculateColumnCenter(myCol);
            if (myCol >= 0 && myCol < TOTAL_COLUMNS && _columnSafeStatus[myCol])
            {
                if (Vector3.Distance(new Vector3(myCenter.x, 0, myCenter.z), Vector3.zero) < 4f) return Vector3.zero;
            }

            Vector3 bestSafePos = Vector3.zero;
            float bestScore = -9999f;

            for (int i = 0; i < TOTAL_COLUMNS; i++)
            {
                if (_columnSafeStatus[i])
                {
                    Vector3 center = CalculateColumnCenter(i);
                    center.y = playerPos.y;
                    float distToMe = Vector3.Distance(playerPos, center);
                    float distToCenter = Vector3.Distance(new Vector3(center.x, 0, center.z), Vector3.zero);
                    float score = (50f - distToCenter * 2f) - distToMe;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSafePos = center;
                    }
                }
            }
            return bestSafePos;
        }

        private static int DetermineColumnIndex(Vector3 pos)
        {
            int col = Mathf.FloorToInt((pos.x + (MAP_WIDTH / 2f)) / COLUMN_WIDTH);
            int row = Mathf.FloorToInt((pos.z + (MAP_WIDTH / 2f)) / COLUMN_WIDTH);
            col = Mathf.Clamp(col, 0, COLUMNS_PER_ROW - 1);
            row = Mathf.Clamp(row, 0, COLUMNS_PER_ROW - 1);
            return col + row * COLUMNS_PER_ROW;
        }

        private static Vector3 CalculateColumnCenter(int index)
        {
            int col = index % COLUMNS_PER_ROW;
            int row = index / COLUMNS_PER_ROW;
            float x = (col * COLUMN_WIDTH) - ((MAP_WIDTH / 2f) - (COLUMN_WIDTH / 2f));
            float z = (row * COLUMN_WIDTH) - ((MAP_WIDTH / 2f) - (COLUMN_WIDTH / 2f));
            return new Vector3(x, 0, z);
        }

        private static void HandleUnstuckSystem(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player)
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= 0.2f)
            {
                _checkTimer = 0f;
                if (Vector3.Distance(player.transform.position, _lastCheckPos) < 0.2f) _stuckTimer += 0.2f;
                else _stuckTimer = 0f;
                _lastCheckPos = player.transform.position;
            }

            if (_stuckTimer >= 0.6f)
            {
                _stuckTimer = 0f;
                _unstuckModeTimer = 0.8f;
                Vector3 forward = player.transform.forward;
                forward.y = 0;
                Vector3 sideDir = Vector3.Cross(forward, Vector3.up).normalized;
                _unstuckDir = (UnityEngine.Random.value > 0.5f ? sideDir : -sideDir);
            }
            if (_unstuckModeTimer > 0) _unstuckModeTimer -= Time.deltaTime;
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique FindClosestEnemy(ulong myId, MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique myMovement)
        {
            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return null;
            MonoBehaviourPublicCSstReshTrheObplBojuUnique closestEnemy = null;
            float minDist = float.MaxValue;
            Vector3 myEyePos = Camera.main != null ? Camera.main.transform.position : myMovement.transform.position + Vector3.up * 1.5f;

            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var enemy = kvp.Value;
                if (enemy == null || enemy.dead || enemy.transform == null) continue;
                float dist = Vector3.Distance(myMovement.transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    Vector3 enemyChest = enemy.transform.position + Vector3.up * 1.0f;
                    if (!Physics.Linecast(myEyePos, enemyChest, out RaycastHit hit) || hit.collider.transform.root == enemy.transform.root)
                    {
                        minDist = dist;
                        closestEnemy = enemy;
                    }
                }
            }
            return closestEnemy;
        }

        private static void ExecuteMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 dir, bool isDodge)
        {
            float currentSpeed = isDodge ? 25f : runSpeed;
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = new Vector3(dir.x * currentSpeed, rb.velocity.y, dir.z * currentSpeed);
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlayRedLight
    {
        public static bool enabled = false;
        public static float runSpeed = 13f;
        private static Vector3 _randomTargetPos = Vector3.zero;
        private static bool _wasRedLight = true;

        public static void Update()
        {
            if (!enabled) return;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return;
            if (LobbyManager.Instance.gameMode.id != 1) return;

            Vector3 myPos = myMovement.transform.position;

            if (myPos.z >= 135f)
            {
                StopMovement(myMovement);
                return;
            }

            bool isGreenLight = IsGreenLight();

            if (!isGreenLight)
            {
                _wasRedLight = true;
                StopMovement(myMovement);

                var target = FindClosestEnemy(myId, myMovement);
                if (target != null)
                {
                    float distToEnemy = Vector3.Distance(myMovement.transform.position, target.transform.position);
                    if (distToEnemy < 6f && Time.frameCount % 15 == 0)
                    {
                        ulong targetId = target.steamProfile.m_SteamID;
                        Vector3 pushDir = (target.transform.position - myMovement.transform.position).normalized;
                        pushDir.y = 0.8f;
                        try { MonoBehaviourPublicInpabyInInInUnique.PunchPlayer(targetId, pushDir * 3.5f); } catch { }
                    }
                }
            }
            else
            {
                if (_wasRedLight)
                {
                    float randomX = UnityEngine.Random.Range(-20f, 20f);
                    _randomTargetPos = new Vector3(randomX, 0, 145f);
                    _wasRedLight = false;
                }

                Vector3 moveDir = (_randomTargetPos - myPos).normalized;
                moveDir.y = 0;
                ExecuteMovement(myMovement, moveDir);
            }
        }

        private static bool IsGreenLight()
        {
            GameObject statue = GameObject.Find("Statue");
            if (statue != null)
            {
                var script = statue.GetComponent<RedLightGreenLightStatue>();
                if (script != null) return script.field_Private_Quaternion_2 != Quaternion.identity;
            }
            return false;
        }

        private static void StopMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }

        private static void ExecuteMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 targetDir)
        {
            Vector3 finalDir = targetDir;
            Vector3 checkPos = player.transform.position + Vector3.up * 0.5f;

            if (Physics.Raycast(checkPos, finalDir, 2f))
            {
                Vector3 leftDir = Quaternion.Euler(0, -45, 0) * finalDir;
                Vector3 rightDir = Quaternion.Euler(0, 45, 0) * finalDir;
                if (!Physics.Raycast(checkPos, leftDir, 2f)) finalDir = leftDir;
                else if (!Physics.Raycast(checkPos, rightDir, 2f)) finalDir = rightDir;
            }

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector2 realSpeed = new Vector2(rb.velocity.x, rb.velocity.z);
                if (realSpeed.magnitude < 2f && player.field_Private_Boolean_3) player.Jump();
                rb.velocity = new Vector3(finalDir.x * runSpeed, rb.velocity.y, finalDir.z * runSpeed);
            }
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique FindClosestEnemy(ulong myId, MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique myMovement)
        {
            if (GameManager.Instance == null || GameManager.Instance.activePlayers == null) return null;
            MonoBehaviourPublicCSstReshTrheObplBojuUnique closestEnemy = null;
            float minDist = float.MaxValue;
            foreach (var kvp in GameManager.Instance.activePlayers)
            {
                if (kvp.Key == myId) continue;
                var enemy = kvp.Value;
                if (enemy == null || enemy.dead || enemy.transform == null) continue;
                float dist = Vector3.Distance(myMovement.transform.position, enemy.transform.position);
                if (dist < minDist) { minDist = dist; closestEnemy = enemy; }
            }
            return closestEnemy;
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlayCrabFight
    {
        public static bool enabled = false;
        public static float runSpeed = 22f;
        private static GameObject _crabBoss = null;
        private static float _dodgeCooldown = 0f;
        private static Vector3 _currentDodgeDir = Vector3.zero;

        public static void Update()
        {
            if (!enabled) return;

            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return;
            if (LobbyManager.Instance.gameMode.id != 18) return;

            _dodgeCooldown -= Time.deltaTime;
            Vector3 myPos = myMovement.transform.position;
            if (_crabBoss == null) _crabBoss = GameObject.Find("Crab") ?? GameObject.Find("CrabBoss");

            var inv = UnityEngine.Object.FindObjectOfType<MonoBehaviourPublicItDi2ObIninInTrweGaUnique>();
            bool hasSnowball = inv != null && inv.currentItem != null;

            if (hasSnowball && _crabBoss != null)
            {
                inv.currentItem.transform.position = _crabBoss.transform.position + new Vector3(0, 3.5f, 0);
                inv.UseItem();
            }

            if (_crabBoss != null)
            {
                Vector3 dirToCrab = (_crabBoss.transform.position - myPos).normalized;
                dirToCrab.y = 0;
                float distToCrab = Vector3.Distance(myPos, _crabBoss.transform.position);

                if (distToCrab < 15f) ExecuteMovement(myMovement, -dirToCrab, false);
                else if (distToCrab > 25f) ExecuteMovement(myMovement, dirToCrab, false);
                else
                {
                    Vector3 strafeDir = Vector3.Cross(dirToCrab, Vector3.up);
                    ExecuteMovement(myMovement, strafeDir, false);
                }
            }
        }

        private static void ExecuteMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 dir, bool isDodge)
        {
            float currentSpeed = isDodge ? 28f : runSpeed;
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = new Vector3(dir.x * currentSpeed, rb.velocity.y, dir.z * currentSpeed);
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlayBustlingButtons
    {
        public static bool enabled = false;
        public static float runSpeed = 14f;
        private static GameObject _targetButton = null;
        private static float _interactCooldown = 0f;

        public static void Update()
        {
            if (!enabled) return;

            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return;
            if (LobbyManager.Instance.gameMode.id != 12) return;

            _interactCooldown -= Time.deltaTime;

            if (_targetButton == null || !_targetButton.activeInHierarchy)
            {
                int badId = GetBadButtonId();
                if (badId < 0) return;
                _targetButton = FindSafeUnpressedButton(badId);
            }

            if (_targetButton != null)
            {
                Vector3 myPos = myMovement.transform.position;
                Vector3 btnPos = _targetButton.transform.position;
                float dist = Vector3.Distance(myPos, btnPos);

                if (dist > 3f)
                {
                    Vector3 moveDir = (btnPos - myPos).normalized;
                    moveDir.y = 0;
                    ExecuteMovement(myMovement, moveDir);
                }
                else
                {
                    var rb = myMovement.GetComponent<Rigidbody>();
                    if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);

                    if (_interactCooldown <= 0f)
                    {
                        try { _targetButton.GetComponent<BustlingButtonsButton>().TryInteract(); } catch { }
                        _interactCooldown = 1.0f;
                        _targetButton = null;
                    }
                }
            }
        }

        private static void ExecuteMovement(MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique player, Vector3 dir)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector2 realSpeed = new Vector2(rb.velocity.x, rb.velocity.z);
                if (realSpeed.magnitude < 3f && player.field_Private_Boolean_3) player.Jump();
                rb.velocity = new Vector3(dir.x * runSpeed, rb.velocity.y, dir.z * runSpeed);
            }
        }

        private static int GetBadButtonId()
        {
            var gameMode = UnityEngine.Object.FindObjectOfType<GameModeBustlingButtons>();
            if (gameMode == null) return -1;
            var gmType = gameMode.GetIl2CppType();
            var flags = Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic;
            var fields = gmType.GetFields(flags);
            if (fields == null) return -1;

            foreach (var f in fields)
            {
                if (f == null || f.FieldType == null || f.FieldType.Name != "Int32") continue;

                string fieldName = f.Name == null ? string.Empty : f.Name.ToLower();
                bool nameHit = fieldName.Contains("bad") || fieldName.Contains("danger") || fieldName.Contains("unsafe") || fieldName.Contains("bomb");
                if (!nameHit) continue;

                int candidate = -1;
                try { candidate = f.GetValue(gameMode).Unbox<int>(); } catch { continue; }
                if (candidate < 0) continue;
                if (!IsExistingButtonId(candidate)) continue;
                return candidate;
            }
            return -1;
        }

        private static bool IsExistingButtonId(int candidate)
        {
            var allObjs = UnityEngine.Object.FindObjectsOfType<GameObject>();
            if (allObjs == null) return false;
            foreach (var obj in allObjs)
            {
                if (obj == null || obj.name != "Button") continue;
                try
                {
                    var btnScript = obj.GetComponent<BustlingButtonsButton>();
                    if (btnScript != null && btnScript.buttonId == candidate) return true;
                }
                catch { }
            }
            return false;
        }

        private static GameObject FindSafeUnpressedButton(int badId)
        {
            var allObjs = UnityEngine.Object.FindObjectsOfType<GameObject>();
            var safeBtns = new List<GameObject>();
            foreach (var obj in allObjs)
            {
                if (obj.name == "Button")
                {
                    try
                    {
                        var btnScript = obj.GetComponent<BustlingButtonsButton>();
                        if (btnScript != null && btnScript.active && btnScript.buttonId != badId) safeBtns.Add(obj);
                    }
                    catch { }
                }
            }
            return safeBtns.Count > 0 ? safeBtns[UnityEngine.Random.Range(0, safeBtns.Count)] : null;
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlayTheFloorIsLava
    {
        public static bool enabled = false;
        private static List<GameObject> _allTiles = new List<GameObject>();
        private static GameObject _targetTile = null;
        private static GameObject _standingTile = null;
        private static readonly HashSet<int> _visitedTiles = new HashSet<int>();
        private static float _timeOnTile = 0f;
        private static float _maxTimeOnTile = 1.0f;
        private static bool _isJumping = false;
        private static float _scanTimer = 0f;

        public static void Update()
        {
            if (!enabled) return;

            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null) return;
            if (LobbyManager.Instance.gameMode.id != 8)
            {
                _visitedTiles.Clear();
                _allTiles.Clear();
                _targetTile = null;
                _standingTile = null;
                return;
            }

            Vector3 myPos = myMovement.transform.position;
            _scanTimer += Time.deltaTime;
            if (_scanTimer > 0.2f)
            {
                _scanTimer = 0f;
                RefreshTiles(myPos.y);
            }

            if (_allTiles.Count == 0) return;

            _standingTile = null;
            foreach (var tile in _allTiles)
            {
                if (tile == null) continue;
                if (Vector2.Distance(new Vector2(myPos.x, myPos.z), new Vector2(tile.transform.position.x, tile.transform.position.z)) < 0.9f)
                {
                    _standingTile = tile;
                    break;
                }
            }

            _timeOnTile += Time.deltaTime;
            if (_targetTile == null || _timeOnTile > _maxTimeOnTile || !_targetTile.activeInHierarchy || _targetTile == _standingTile)
            {
                if (_standingTile != null)
                {
                    if (_visitedTiles.Count > 200) _visitedTiles.Clear();
                    _visitedTiles.Add(_standingTile.GetInstanceID());
                }

                GameObject nextTile = null;
                foreach (var tile in _allTiles.OrderBy(t => Vector3.Distance(myPos, t.transform.position)))
                {
                    if (tile == null || !tile.activeInHierarchy) continue;
                    if (tile == _standingTile) continue;
                    if (_visitedTiles.Contains(tile.GetInstanceID())) continue;
                    nextTile = tile;
                    break;
                }
                if (nextTile == null)
                {
                    foreach (var tile in _allTiles.OrderBy(t => Vector3.Distance(myPos, t.transform.position)))
                    {
                        if (tile == null || !tile.activeInHierarchy) continue;
                        if (tile == _standingTile) continue;
                        nextTile = tile;
                        break;
                    }
                }

                _timeOnTile = 0f;
                _isJumping = false;
                _maxTimeOnTile = UnityEngine.Random.Range(0.8f, 1.5f);

                if (nextTile == null)
                {
                    _targetTile = null;
                    var stopRb = myMovement.GetComponent<Rigidbody>();
                    if (stopRb != null) stopRb.velocity = new Vector3(0, stopRb.velocity.y, 0);
                    return;
                }

                _targetTile = nextTile;
                _visitedTiles.Add(nextTile.GetInstanceID());
            }

            if (_targetTile != null)
            {
                float dist = Vector2.Distance(new Vector2(myPos.x, myPos.z), new Vector2(_targetTile.transform.position.x, _targetTile.transform.position.z));
                var rb = myMovement.GetComponent<Rigidbody>();
                if (dist > 0.8f)
                {
                    if (!_isJumping && myMovement.field_Private_Boolean_3)
                    {
                        myMovement.Jump();
                        _isJumping = true;
                    }
                    Vector3 moveDir = (_targetTile.transform.position - myPos).normalized;
                    moveDir.y = 0;
                    if (rb != null) rb.velocity = new Vector3(moveDir.x * 16f, rb.velocity.y, moveDir.z * 16f);
                }
                else
                {
                    if (rb != null) rb.velocity = new Vector3(0, rb.velocity.y, 0);
                    if (myMovement.field_Private_Boolean_3) _isJumping = false;
                }
            }
        }

        private static void RefreshTiles(float playerY)
        {
            _allTiles.Clear();
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null || !obj.activeInHierarchy) continue;
                if (obj.name.Contains("SafePiece") && obj.transform.position.y > playerY - 3f)
                {
                    _allTiles.Add(obj);
                }
            }
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }

    public static class AutoPlaySteppingStones
    {
        public static bool enabled = false;
        private static List<Vector3> allTiles = new List<Vector3>();
        private static bool init = false;
        private static bool hasJumped = false;
        private static float elapsedTime = 0f;
        private static float elapsedNextClosestTile = 2.5f;
        private static float timer = 3f;
        private static int nextTileIndex = 0;
        private static Vector3 closestTile = Vector3.zero;
        private static bool hasClosestTile = false;

        public static void Update()
        {
            if (!enabled) return;

            var myPlayerManager = GetLocalPlayerManager();
            if (myPlayerManager == null || myPlayerManager.dead) return;

            var myMovement = myPlayerManager.GetComponent<MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique>();
            if (myMovement == null) return;

            if (LobbyManager.Instance == null || LobbyManager.Instance.gameMode == null || LobbyManager.Instance.gameMode.id != 3)
            {
                init = false;
                allTiles.Clear();
                nextTileIndex = 0;
                hasJumped = false;
                hasClosestTile = false;
                closestTile = Vector3.zero;
                return;
            }

            Vector3 playerPos = myMovement.transform.position;
            elapsedTime += Time.deltaTime;
            elapsedNextClosestTile += Time.deltaTime;

            if (!init && elapsedTime > 0.1f)
            {
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("PieceSolid") || obj.name.Contains("GlassSolid"))
                    {
                        allTiles.Add(obj.transform.position);
                    }
                }
                allTiles = allTiles.OrderBy(pos => Vector3.Distance(pos, playerPos)).ToList();
                nextTileIndex = 0;
                hasJumped = false;
                hasClosestTile = allTiles.Count > 0;
                if (hasClosestTile) closestTile = allTiles[0];
                init = true;
            }

            if (!init || allTiles.Count == 0 || !hasClosestTile) return;

            if (elapsedNextClosestTile > timer && nextTileIndex < allTiles.Count)
            {
                if (Vector3.Distance(playerPos, allTiles[nextTileIndex]) < 3f)
                {
                    nextTileIndex++;
                    hasJumped = false;
                }
                if (nextTileIndex < allTiles.Count) closestTile = allTiles[nextTileIndex];
                elapsedNextClosestTile = 0f;
                timer = UnityEngine.Random.Range(1f, 3f);
            }

            Vector3 targetPosition = closestTile + new Vector3(0, 2.5f, 0);
            if (!hasJumped && myMovement.field_Private_Boolean_3)
            {
                myMovement.Jump();
                hasJumped = true;
            }

            float dist = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(closestTile.x, closestTile.z));
            var rb = myMovement.GetComponent<Rigidbody>();
            if (dist > 1.0f)
            {
                Vector3 diff = (targetPosition - playerPos).normalized;
                diff.y = 0;
                if (rb != null) rb.velocity = new Vector3(diff.x * 18f, rb.velocity.y, diff.z * 18f);
            }
            else if (rb != null && myMovement.field_Private_Boolean_3)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }

        private static MonoBehaviourPublicCSstReshTrheObplBojuUnique GetLocalPlayerManager()
        {
            ulong myId = SteamManager.Instance != null ? SteamManager.Instance.field_Private_CSteamID_0.m_SteamID : 0;
            if (GameManager.Instance != null && GameManager.Instance.activePlayers.ContainsKey(myId))
                return GameManager.Instance.activePlayers[myId];
            return null;
        }
    }
}
