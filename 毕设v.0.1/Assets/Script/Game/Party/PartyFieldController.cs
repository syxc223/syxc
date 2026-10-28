using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

public class PartyFieldController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followersParent;
    [SerializeField] private GameObject fieldFollowerPrefab;
    [SerializeField] private Transform playerTrans;

    [Header("Settings")]
    [SerializeField] private float followDistance = 1.2f;
    [SerializeField] private float followSpeed = 5f;

    [SerializeField] private float zOffset = 0.01f;
    [SerializeField] private float sampleMinDistance = 0.05f; // 最小采样值

    private List<Vector3> trail = new(); // 记录移动轨迹
    private List<FieldFollower> fieldFollowers = new(); // 跟随者


    void LateUpdate()
    {
        UpdateLeaderTrail();

        for (int i = 0; i < fieldFollowers.Count; i++)
        {
            var follower = fieldFollowers[i];
            float targetDistance = followDistance * (i + 1);
            Vector3 targetPos = GetPointAtDistance(targetDistance);
            follower.MoveTo(ApplyFollowerOffset(targetPos, i), followSpeed);
        }
    }

    private Vector3 GetPointAtDistance(float distanceFromLeader)
    {
        if (trail.Count == 0) return playerTrans.position;//TODO: 需要修改
        //if (trail.Count == 1) return trail[0];//TODO: 需要修改

        float accumulated = 0f; // 累计距离

        for (int i = 0; i < trail.Count - 1; i++)
        {
            Vector3 a = trail[i];
            Vector3 b = trail[i + 1];

            float dist = Vector3.Distance(a, b);
            if (dist <= Mathf.Epsilon)
                continue;

            if (accumulated + dist >= distanceFromLeader)
            {
                float t = (distanceFromLeader - accumulated) / dist;
                return Vector3.Lerp(a, b, t);
            }
            accumulated += dist;
        }
        return trail[^1];
    }

    public void UpdateFollowers(List<CharacterDefinitionSO> partyMembers)
    {
        int followerCount = partyMembers.Count - 1; // 减去主角
        while (fieldFollowers.Count < followerCount)
        {
            int index = fieldFollowers.Count;
            var pos = ApplyFollowerOffset(playerTrans.position, index);
            GameObject followerObj = Instantiate(fieldFollowerPrefab, pos, Quaternion.identity, followersParent);
            fieldFollowers.Add(followerObj.GetComponent<FieldFollower>());
        }

        for (int i = 0; i < followerCount; i++)
        {
            fieldFollowers[i].SetupFollower(partyMembers[i + 1]);
        }
    }

    private Vector3 ApplyFollowerOffset(Vector3 position, int index)
    {
        position.z += zOffset * (index + 1);
        return position;
    }
    private void UpdateLeaderTrail()
{
    Vector3 leaderPos = playerTrans.position;

    if (trail.Count == 0)
    {
        trail.Add(leaderPos);
        return;
    }

    float dist = Vector3.Distance(leaderPos, trail[0]);
    if (dist > sampleMinDistance)
    {
        trail.Insert(0, leaderPos);

        if (trail.Count > 30)
            trail.RemoveAt(trail.Count - 1); // 最多记录30个点
    }
}

    private void RebuildTrailAndSnapFollowers()
{
    trail.Clear();

    for (int i = 0; i < fieldFollowers.Count; i++)
        fieldFollowers[i].SnapTo(ApplyFollowerOffset(playerTrans.position, i));

    UpdateLeaderTrail();
}
}