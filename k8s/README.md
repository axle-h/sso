# Kubernetes Deployment

I use k3s. This might not work otherwise.

Client secrets and the initial user password are read from a secret named `sso`, they are
no longer generated on startup and logged.

```shell
# Create the namespace
kubectl create namespace sso

# Create the secrets, these are the shared secrets configured in each client app
kubectl -n sso create secret generic sso \
  --from-literal=Clients__make-money__Secret="$(uuidgen)" \
  --from-literal=Clients__make-movies__Secret="$(uuidgen)" \
  --from-literal=Clients__risk__Secret="$(uuidgen)" \
  --from-literal=Users__0__Password='<initial password>'

kubectl -n sso apply -f *

# Read a client secret back out to configure the client app with
kubectl -n sso get secret sso -o jsonpath='{.data.Clients__risk__Secret}' | base64 -d
```

`Users__0__Password` only applies when the user does not exist yet. Changing it later will
not reset an existing password, use `/ChangePassword` for that.
