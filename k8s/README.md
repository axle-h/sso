# Kubernetes Deployment

I use k3s. This might not work otherwise.

```shell
# Create the namespace
kubectl create namespace sso
kubectl -n sso apply -f .

# Get the generated client credentials
kubectl -n sso logs sso-7f85867d4b-dlrz7 | grep secret:
```