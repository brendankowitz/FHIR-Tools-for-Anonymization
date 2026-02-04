# FHIR Tools for Anonymization - Agent Goals

## Mission Statement

**Protect patient privacy while enabling healthcare innovation.**

This toolkit enables organizations to safely anonymize FHIR and DICOM healthcare data for secondary use cases including research, public health analytics, and data sharing. Our agents exist to maintain, modernize, and elevate this critical healthcare infrastructure to first-class status.

---

## Project Context

| Aspect | Detail |
|--------|--------|
| **Domain** | Healthcare data anonymization (HIPAA Safe Harbor compliant) |
| **Formats** | FHIR, DICOM |
| **Language** | C# / .NET |
| **Origin** | Microsoft Healthcare Team (open-sourced March 2020) |
| **Status** | Production-ready, actively maintained, community-driven |

**Privacy Commitment**: This tool does NOT access, collect, or manage any user data. Users bring their own data and retain full responsibility.

---

## Strategic Goals

### 1. Security Excellence (Highest Priority)

Healthcare data demands the highest security standards. Agents must:

- **Proactively identify vulnerabilities** in anonymization algorithms
- **Review all PRs** for security implications before merge
- **Monitor dependencies** for CVEs and security advisories
- **Ensure HIPAA compliance** is maintained across all changes
- **Validate cryptographic implementations** (crypto-hash, encrypt methods)
- **Audit configuration handling** to prevent data leakage

**Key Areas**:
- `Microsoft.Health.*.Anonymizer.Core` - Core anonymization logic
- Crypto-hash and encryption implementations
- Configuration file parsing and validation

### 2. Modernization & Technical Excellence

Bring the codebase to modern .NET standards:

- **Maintain multi-targeting** for .NET (most recent version and most recent LTS version)
- **Adopt modern C# features** (records, pattern matching, spans)
- **Improve performance** through benchmarking and optimization
- **Reduce technical debt** identified in code reviews
- **Enhance logging** with structured logging patterns
- **Add telemetry** for operational insights (opt-in, privacy-respecting)

### 3. Feature Completeness

Address documented limitations to reach feature parity:

| Gap | Priority | Notes |
|-----|----------|-------|
| XML format support | High | Currently JSON/NDJSON only |
| Extensions field anonymization | High | Not currently supported |
| DICOM pixel data anonymization | Medium | Currently metadata only |
| Sequence of Items full support | Medium | Only redact/remove available |

### 4. Quality & Reliability

Maintain confidence in anonymization correctness:

- **Expand test coverage** beyond current 48% test file ratio
- **Add property-based testing** for anonymization algorithms
- **Implement integration tests** with real-world FHIR/DICOM samples
- **Validate compliance** against HIPAA Safe Harbor requirements
- **Add regression tests** for every bug fix
- **Benchmark anonymization performance** across data sizes

### 5. Documentation & Developer Experience

Lower the barrier to contribution:

- **Keep documentation synchronized** with code changes
- **Add inline code documentation** for complex algorithms
- **Improve error messages** with actionable guidance
- **Create troubleshooting guides** for common issues
- **Document architecture decisions** via ADRs

### 6. Community Health

Foster an active, welcoming community:

- **Triage issues promptly** with clear labels and priorities
- **Respond to questions** with helpful context
- **Review PRs constructively** with actionable feedback
- **Identify and close stale issues** with proper resolution
- **Recognize contributors** and encourage participation

---

## Architectural Principles

Agents must respect these principles in all decisions:

1. **Privacy by Design** - Default to the most private option
2. **Correctness over Speed** - Anonymization must be correct; performance is secondary
3. **Configuration-Driven** - Behavior controlled by configuration, not code changes
4. **Backward Compatibility** - Existing configurations must continue to work
5. **Minimal Dependencies** - Healthcare environments have strict dependency policies
6. **Audit Trail** - All anonymization operations should be traceable
7. **Fail Secure** - On error, do not emit potentially identifiable data

---

## Code Quality Standards

### Required for All Changes

- [ ] All tests pass (`dotnet test`)
- [ ] No new compiler warnings
- [ ] Nullable reference types handled correctly
- [ ] XML documentation on public APIs
- [ ] Configuration changes are backward compatible

### Security Review Required For

- Changes to anonymization algorithms
- Cryptographic implementations
- Configuration parsing logic
- Input validation routines
- New dependencies

---

## Priority Matrix

| Issue Type | Response Time | Agent Action |
|------------|---------------|--------------|
| Security vulnerability | Immediate | Create fix PR, notify maintainers |
| Bug in anonymization | High | Investigate, create fix with tests |
| Dependency update (security) | High | Validate, test, create PR |
| Feature request | Normal | Assess alignment, create investigation |
| Documentation gap | Normal | Create update PR |
| Performance issue | Normal | Benchmark, propose optimization |
| Dependency update (non-security) | Low | Batch with other updates |

---

## Agent Behavioral Guidelines

### Do

- Verify anonymization correctness with test cases
- Consider edge cases in healthcare data (missing fields, nested resources)
- Respect the FHIR and DICOM specifications
- Maintain compatibility with Azure Data Factory pipeline
- Test against all supported .net versions

### Do Not

- Introduce dependencies without security review
- Modify anonymization behavior without extensive testing
- Break existing configuration file formats
- Skip validation even for "simple" changes

---

## Success Metrics

Agents should work toward:

- **Zero security vulnerabilities** in anonymization logic
- **100% test coverage** on core anonymization methods
- **< 24 hour response** to new issues
- **< 1 week** to merge security patches
- **All dependencies** within 1 minor version of latest
- **Documentation accuracy** verified quarterly

---

## Key Files Reference

| Purpose | Location |
|---------|----------|
| FHIR Core | `FHIR/src/Microsoft.Health.Fhir.Anonymizer.*.Core/` |
| Shared Logic | `FHIR/src/Microsoft.Health.Fhir.Anonymizer.Shared.Core/` |
| DICOM Core | `DICOM/src/Microsoft.Health.Dicom.Anonymizer.Core/` |
| Sample Configs | `FHIR/src/*/configuration-sample.json` |
| Build Config | `Directory.Build.props` |
| CI/CD | `.github/workflows/` |
| Documentation | `docs/` |

---

## Anonymization Methods Reference

Agents should understand these methods when reviewing code:

| Method | Purpose | Security Sensitivity |
|--------|---------|---------------------|
| `redact` | Remove data element entirely | Low |
| `dateShift` | Shift dates by consistent random offset | Medium |
| `cryptoHash` | One-way hash with salt | High |
| `encrypt` | Reversible encryption | Critical |
| `substitute` | Replace with synthetic value | Medium |
| `perturb` | Add noise to numeric values | Medium |
| `generalize` | Reduce precision (e.g., age ranges) | Medium |
| `keep` | Preserve original value | Review Required |

---

*This document guides autonomous agents in maintaining and improving the FHIR Tools for Anonymization project. Last updated: 2025-02-03*
