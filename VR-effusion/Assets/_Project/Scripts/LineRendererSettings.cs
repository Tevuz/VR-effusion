using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace VREffusion {
    public class LineRendererSet : MonoBehaviour {
        public GameObject panel;
        public Image img;
        public Button btn;

        [SerializeField] LineRenderer lineRenderer;

        Vector3[] points;

        public LayerMask layerMask;

        // Start is called before the first frame update
        void Start() {
            img = panel.GetComponent<Image>();
            lineRenderer = gameObject.GetComponent<LineRenderer>();

            points = new Vector3[2];

            points[1] = Vector3.zero;

            points[1] = transform.position + new Vector3(0, 0, 20);

            lineRenderer.SetPositions(points);
            lineRenderer.enabled = true;
        }

        public bool AlignLineRenderer(LineRenderer renderer) {
            bool hitBtn = false;
            Ray ray;
            ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;


            if (Physics.Raycast(ray, out hit, layerMask)) {


                points[1] = transform.forward + new Vector3(0, 0, hit.distance);
                btn = hit.collider.gameObject.GetComponent<Button>();

                hitBtn = true;

            } else {
                points[1] = transform.forward + new Vector3(0, 0, 20);
            }

            renderer.SetPositions(points);
            return hitBtn;
        }

        public TMP_Text feedbackText; // Dra Text UI-element hit i Inspector

        // Denne metoden laster en scene basert p� dens navn
        public void LoadScene(string sceneName) {
            SceneManager.LoadScene(sceneName);
        }

        // Denne metoden viser en midlertidig tilbakemelding
        public void ShowFeedback(string message) {
            StartCoroutine(ShowFeedbackCoroutine(message));
        }

        private IEnumerator ShowFeedbackCoroutine(string message) {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2); // Vent i 2 sekunder
            feedbackText.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update() {
            if (AlignLineRenderer(lineRenderer) && Input.GetAxis("Submit") > 0) {
                btn.onClick.Invoke();
            }
        }
    }
}
