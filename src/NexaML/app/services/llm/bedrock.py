import json

from app.domain.interfaces import ILLMService
from app.config import settings


class BedrockLLMService(ILLMService):
    """
    Genera insights en lenguaje natural usando AWS Bedrock (Claude).
    boto3 se importa de forma lazy para no penalizar tests unitarios.
    """

    async def generate_insight(self, context: dict) -> str:
        import boto3  # lazy — not needed in unit tests

        client = boto3.client("bedrock-runtime", region_name=settings.aws_region)
        prompt = self._build_prompt(context)

        body = json.dumps({
            "anthropic_version": "bedrock-2023-05-31",
            "max_tokens": 512,
            "messages": [{"role": "user", "content": prompt}],
        })

        response = client.invoke_model(
            modelId=settings.bedrock_model_id,
            body=body,
            contentType="application/json",
            accept="application/json",
        )

        result = json.loads(response["body"].read())
        return result["content"][0]["text"].strip()

    def _build_prompt(self, context: dict) -> str:
        return f"""Eres un analista de negocios para pequeñas empresas (barberías, restaurantes).
Analiza los siguientes datos y genera un insight accionable en español, máximo 3 oraciones.
Sé directo y específico. No uses jerga técnica.

Datos del negocio:
- Ticket promedio: ${context.get('avg_ticket', 0):.2f}
- Ventas últimos 30 días: ${context.get('total_revenue_30d', 0):.2f}
- Días con anomalías detectadas: {context.get('anomaly_count', 0)}
- Predicción próximos 7 días: ${context.get('forecast_7d_total', 0):.2f}
- Producto más vendido: {context.get('top_product', 'N/A')}

Genera el insight:"""
