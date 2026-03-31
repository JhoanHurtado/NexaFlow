FROM public.ecr.aws/lambda/python:3.12

WORKDIR ${LAMBDA_TASK_ROOT}

# Instalar dependencias del sistema para Prophet/numpy
RUN dnf install -y gcc gcc-c++ && dnf clean all

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app/ ./app/

CMD ["app.main.handler"]
