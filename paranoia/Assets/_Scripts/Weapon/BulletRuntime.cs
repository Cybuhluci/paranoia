using UnityEngine;

public class BulletRuntime : MonoBehaviour
{
    // layers to ignore during raycast (e.g., the shooter, the gun, etc.):
    [SerializeField] LayerMask ignoreLayers;

    private CalibreSO calibreSO;
    private Vector3 velocity;
    private float gravity = -9.81f; // can be overridden per-calibre later (e.g. rocket-propelled calibres).

    private float weight; // in grammes, affects how much gravity/drag pulls on the bullet's trajectory.
    private const float referenceWeight = 8f; // roughly a 9mm bullet's weight, used as the baseline for trajectory scaling.

    private bool isLaunched;
    private float builtInDeathCounter = 60f; // how long a bullet can fly in seconds before being destroyed, to prevent forever bullets.

    [SerializeField] private ImpactPrefabs[] impactPrefabs; // different prefabs for different impact types (e.g. metal, wood, flesh, etc.).

    public void Launch(Vector3 direction, float muzzleVelocity, CalibreSO calibreSO)
    {
        this.calibreSO = calibreSO;
        velocity = direction.normalized * muzzleVelocity;
        weight = calibreSO.weight > 0f ? calibreSO.weight : referenceWeight;
        isLaunched = true;
        impactPrefabs = calibreSO.impactPrefabs;
    }

    private void Update()
    {
        if (!isLaunched)
        {
            return;
        }

        // heavier bullets are affected more by gravity (drop faster/sooner), lighter bullets less so.
        float weightScale = weight / referenceWeight;
        velocity.y += gravity * weightScale * Time.deltaTime;

        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + velocity * Time.deltaTime;
        Vector3 travelDirection = nextPosition - currentPosition;
        float travelDistance = travelDirection.magnitude;

        if (travelDistance > 0f && Physics.Raycast(currentPosition, travelDirection.normalized, out RaycastHit hit, travelDistance, ~ignoreLayers))
        {
            OnHit(hit);
            return;
        }

        transform.position = nextPosition;
        transform.rotation = Quaternion.LookRotation(velocity.normalized);

        // built-in death counter to prevent bullets from flying forever.
        builtInDeathCounter -= Time.deltaTime;
        if (builtInDeathCounter <= 0f)
        {
            isLaunched = false;
            Destroy(gameObject);
        }
    }

    private void OnHit(RaycastHit hit)
    {
        // penetration/damage calculations go here in a later phase.
        isLaunched = false;
        // spawn impact effect based on the hit surface type.
        if (hit.collider != null)
        {
            ImpactType impactType = ImpactType.ExtraNone; // default impact type
            if (hit.collider.CompareTag("Metal"))
            {
                impactType = ImpactType.Metal;
            }
            else if (hit.collider.CompareTag("Wood"))
            {
                impactType = ImpactType.Wood;
            }
            else if (hit.collider.CompareTag("Flesh") || hit.collider.CompareTag("Head"))
            {
                impactType = ImpactType.Flesh;
            }
            else if (hit.collider.CompareTag("Concrete"))
            {
                impactType = ImpactType.Concrete;
            }
            else
            {
                impactType = ImpactType.Concrete; // default to concrete if no specific tag is found
            }
                GameObject impactPrefab = null;
            foreach (var impact in impactPrefabs)
            {
                if (impact.impactType == impactType)
                {
                    impactPrefab = impact.impactPrefab;
                    break;
                }
            }
            if (impactPrefab != null)
            {
                Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForce(velocity * 0.1f, ForceMode.Impulse); // apply a small force to the hit object
        }

        // try and get the AI_Base component from the hit object or its parent, and call its TakeDamage method if it exists.
        AI_BASE aiBase = hit.collider.GetComponent<AI_BASE>() ?? hit.collider.GetComponentInParent<AI_BASE>();
        if (aiBase != null)
        {
            aiBase.TakeDamage(calibreSO.baseDam);
        }

        Destroy(gameObject);
    }
}
