#!/usr/bin/env bash
#https://microk8s.io/#install-microk8s
brew install ubuntu/microk8s/microk8s
sudo microk8s status --wait-ready
microk8s install
microk8s enable dns dashboard registry
sudo usermod -a -G microk8s $USER
sudo chown -f -R $USER ~/.kube
microk8s kubectl get all --all-namespaces
microk8s dashboard-proxy
echo "MicroK8s installation completed. Please log out and log back in for group changes to take effect."
            - name: ENVIRONMENT
              value: "production"
EOF