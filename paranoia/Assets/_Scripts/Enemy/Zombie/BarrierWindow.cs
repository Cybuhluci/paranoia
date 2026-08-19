using UnityEngine;

public class BarrierWindow : MonoBehaviour
{
    public Transform startpoint, endpoint; // the start point is on the outside, the endpoint is on the inside
                                           // - used for "animation" of the zombie going through the window.
    public Transform[] barriers; // the things the zombie takes down to then be able to go through the window.
    public bool allBarriersGone; // simple bool that allows quick checks by zombies.
    public bool anyBarrierMissing; // true if at least one barrier is currently gone - lets the player know this window needs rebuilding.

    public void RemoveBarrier()
    {
        // removes a singular barrier (eg: 5 barriers, select 1-5, if its not gone, then remove it, if not, try again until you can remove one.)
        if (barriers == null || barriers.Length == 0)
        {
            return;
        }

        int startIndex = Random.Range(0, barriers.Length);

        for (int i = 0; i < barriers.Length; i++)
        {
            int index = (startIndex + i) % barriers.Length;
            Transform barrier = barriers[index];

            if (barrier != null && barrier.gameObject.activeSelf)
            {
                barrier.gameObject.SetActive(false);
                return;
            }
        }
    }

    public void RemoveAllBarriers()
    {
        // removes all barriers (eg: 5 barriers, remove them all.)
        if (barriers == null)
        {
            return;
        }

        foreach (Transform barrier in barriers)
        {
            if (barrier != null)
            {
                barrier.gameObject.SetActive(false);
            }
        }
    }

    public void AddBarrier()
    {
        // adds a singular barrier (eg: 5 barriers, select 1-5, if its gone, then add it, if not, try again until you can add one.)
        if (barriers == null || barriers.Length == 0)
        {
            return;
        }

        int startIndex = Random.Range(0, barriers.Length);

        for (int i = 0; i < barriers.Length; i++)
        {
            int index = (startIndex + i) % barriers.Length;
            Transform barrier = barriers[index];

            if (barrier != null && !barrier.gameObject.activeSelf)
            {
                barrier.gameObject.SetActive(true);
                return;
            }
        }
    }

    public void AddAllBarriers()
    {
        // adds all barriers (eg: 5 barriers, add them all.)
        if (barriers == null)
        {
            return;
        }

        foreach (Transform barrier in barriers)
        {
            if (barrier != null)
            {
                barrier.gameObject.SetActive(true);
            }
        }
    }

    private void Update()
    {
        // check if all barriers are gone, if so, set allBarriersGone to true, else set it to false.
        bool allGone = true;
        bool anyGone = false;

        foreach (Transform barrier in barriers)
        {
            if (barrier.gameObject.activeSelf)
            {
                allGone = false;
            }
            else
            {
                anyGone = true;
            }
        }

        allBarriersGone = allGone;
        anyBarrierMissing = anyGone;
    }
}

