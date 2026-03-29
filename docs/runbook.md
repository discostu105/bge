# BGE Production Runbook

Region: `eu-central-1` | Cluster: `bge` | Service: `bge`

---

## Health Check

Run all checks in one pass:

```bash
# ECS service status
aws ecs describe-services --cluster bge --services bge \
  --query 'services[0].{status:status,running:runningCount,desired:desiredCount,deployments:deployments[*].{state:rolloutState,running:runningCount,desired:desiredCount}}'

# ALB target health
TG_ARN=$(aws elbv2 describe-target-groups --names bge --query 'TargetGroups[0].TargetGroupArn' --output text)
aws elbv2 describe-target-health --target-group-arn "$TG_ARN"

# Recent stopped tasks (crashes)
aws ecs list-tasks --cluster bge --desired-status STOPPED --max-items 5 \
  --query 'taskArns' --output text | xargs -I{} \
  aws ecs describe-tasks --cluster bge --tasks {} \
    --query 'tasks[*].{status:lastStatus,reason:stoppedReason,exitCode:containers[0].exitCode}'

# Recent application logs
aws logs tail /ecs/bge --since 30m --format short

# Game state — verify world is ticking (last S3 write < 30s ago)
aws s3api list-object-versions --bucket bge-game-state --prefix "" --max-keys 5 \
  --query 'Versions[*].{Key:Key,LastModified:LastModified,Size:Size}'
```

**Healthy indicators:**
- `runningCount == desiredCount == 1`
- ALB target `State.State == healthy`
- No stopped tasks with non-zero exit codes
- Game state bucket has writes within the last 30 seconds
- No ERROR-level log lines in the last 30 minutes

---

## CloudWatch Dashboard

Open the dashboard for a visual overview:

```
https://eu-central-1.console.aws.amazon.com/cloudwatch/home?region=eu-central-1#dashboards:name=bge
```

Widgets:
- ECS Running Task Count
- ECS CPU & Memory Utilization (%)
- ALB Request Count
- ALB 5xx / 4xx Error Count
- ALB Target Response Time (p50 / p95 / p99)
- ALB Target Health (healthy/unhealthy host count)

---

## Active Alarms

| Alarm | Trigger | Action |
|-------|---------|--------|
| `bge-ecs-task-count-low` | Running tasks < 1 for 1 minute | See "Restart Service" below |
| `bge-alb-5xx-rate-high` | 5xx error rate > 1% over 5 min | Check app logs; see "Investigate Errors" |
| `bge-ecs-memory-high` | Memory > 80% for 2 consecutive 5-min periods | See "Scale Up" below |
| `bge-ecs-cpu-high` | CPU > 80% for 2 consecutive 5-min periods | See "Scale Up" below |

SNS topic: `bge-alarms` — email subscribers receive notifications on state changes.

---

## Restart Service

Force a new ECS deployment (pulls latest image from ECR):

```bash
aws ecs update-service --cluster bge --service bge --force-new-deployment

# Wait for the new task to become healthy
aws ecs wait services-stable --cluster bge --services bge
echo "Service is stable"
```

If the new task fails to start, check logs immediately:

```bash
aws logs tail /ecs/bge --since 5m --format short
```

---

## Rollback to Previous Image

```bash
# Find the previous image tag in ECR
aws ecr describe-images --repository-name bge \
  --query 'sort_by(imageDetails, &imagePushedAt)[-5:].{tag:imageTags[0],pushed:imagePushedAt}' \
  --output table

# Get current task definition
CURRENT_TD=$(aws ecs describe-services --cluster bge --services bge \
  --query 'services[0].taskDefinition' --output text)

# Register a new task definition with the previous image tag
# (replace <previous-tag> with the desired SHA or tag)
REPO_URL=$(aws ecr describe-repositories --repository-names bge \
  --query 'repositories[0].repositoryUri' --output text)

aws ecs register-task-definition \
  --cli-input-json "$(aws ecs describe-task-definition \
    --task-definition bge \
    --query 'taskDefinition' | \
    jq --arg img "$REPO_URL:<previous-tag>" \
      '.containerDefinitions[0].image = $img | del(.taskDefinitionArn,.revision,.status,.requiresAttributes,.compatibilities,.registeredAt,.registeredBy)')"

# Deploy the new (rolled-back) task definition
aws ecs update-service --cluster bge --service bge \
  --task-definition bge  # uses latest registered revision

aws ecs wait services-stable --cluster bge --services bge
```

---

## Scale Up

Increase task CPU/memory requires a new task definition (Fargate sizes are fixed).
For quick relief, increase `desired_count` to run multiple tasks:

```bash
aws ecs update-service --cluster bge --service bge --desired-count 2
aws ecs wait services-stable --cluster bge --services bge
```

> **Note:** BGE is a stateful server — world state is serialized to S3 every 10 seconds.
> Running multiple tasks is safe for serving traffic (ALB load-balances), but each task
> has its own in-memory world state. Only use multiple tasks as a temporary relief measure;
> scale back to 1 once the root cause is addressed.

To permanently increase CPU/memory, update the `aws_ecs_task_definition` resource in
`infra/main.tf` and apply via Terraform.

---

## Investigate Errors

```bash
# Application error logs
aws logs filter-log-events \
  --log-group-name /ecs/bge \
  --filter-pattern "ERROR" \
  --start-time $(date -d '1 hour ago' +%s000) \
  --query 'events[*].message' \
  --output text

# ALB access logs are in S3 (enabled, 30-day retention)
aws s3 ls s3://bge-alb-logs/AWSLogs/ --recursive | sort | tail -20
```

---

## Game State Recovery

World state is serialized to S3 every 10 seconds with versioning enabled.

```bash
# List recent versions
aws s3api list-object-versions --bucket bge-game-state \
  --query 'Versions[*].{VersionId:VersionId,LastModified:LastModified,Key:Key}' \
  --output table

# Restore a specific version (download locally to inspect)
aws s3api get-object \
  --bucket bge-game-state \
  --key <world-state-key> \
  --version-id <version-id> \
  world-state-backup.json
```

The service automatically loads the latest version from S3 on startup.

---

## CI/CD Pipeline

Deployments are triggered by push to `master` via GitHub Actions.

```bash
# Check recent workflow runs
gh run list --workflow=deploy.yml --limit 10

# View a specific run
gh run view <run-id>

# Re-run a failed deployment
gh run rerun <run-id>
```

Pipeline steps: test → build Docker image → push to ECR (tagged with commit SHA + `latest`) → force new ECS deployment.
