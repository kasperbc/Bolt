using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnhookableWall : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Hook"))
        {
            HookBehaviour hook = collision.gameObject.GetComponent<HookBehaviour>();

            Vector2 launchDir = (hook.origin.transform.position - hook.transform.position).normalized;

            collision.gameObject.GetComponent<Rigidbody2D>().AddForce(launchDir * 5, ForceMode2D.Impulse);
            hook.EnableHookGravity();
        }
    }
}
